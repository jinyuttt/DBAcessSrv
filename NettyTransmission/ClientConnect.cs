#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：ClientConnect
* 类 描 述 ：管理客户端多个连接，不能超过CPU线程数
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion

using DotNetty.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：ClientConnect  客户端连接
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class ClientConnect
    {
        /// <summary>
        /// 每个连接最大提交频率
        /// </summary>
        private const int MaxConnectCount = 100;

        private const int MTikcs = 10000;//与毫秒换算

        private const int ShutDownTime =5* 60 * 1000;//5分钟

        #region 客户端状态

        private const int Init = 0;
        private const int Close = 3;
        private const int Use = 1;
        private const int Free = 2;
        private const int Open = 4;
        private readonly Dictionary<NettyClient, NettyClientState> dicState;

        #endregion

       
        private ConcurrentBag<NettyClientState> bagClients = null;

        private volatile bool isCheckClient = false;

        public NettyClientState[] nettyClienStates;

        private NettyClientManager nettyClient = null;

        public NettyAddress Address { get; set; }

        public string ClientFlage { get; set; }


        /// <summary>
        /// 数据回传事件
        /// </summary>
        public event NettyDataNotify DataNotify;
        public ClientConnect(NettyClientManager  nettyClient)
        {
            nettyClienStates = new NettyClientState[Environment.ProcessorCount];
            this.nettyClient = nettyClient;
            dicState = new Dictionary<NettyClient, NettyClientState>();
            bagClients = new ConcurrentBag<NettyClientState>();
        }

        /// <summary>
        /// 初始化连接端
        /// </summary>
        public void InitClient()
        {
            for(int i=0;i<nettyClienStates.Length;i++)
            {
                nettyClienStates[i] = new NettyClientState() { Address = Address, ClientFlage = ClientFlage, CreateTick = DateTime.Now.Ticks, State = Init };
            }
        }
      
        /// <summary>
        /// 找到空闲的客户端
        /// </summary>
        /// <returns></returns>
        public NettyClientState FindFree()
        {
            int len = nettyClienStates.Length;
            for (int i = 0; i < len; i++)
            {
                var c = nettyClienStates[i];
                if (c.State == Free && c.IsConnected)
                {
                    c.State = Use;
                    return c;
                }

            }
            return null;
        }

        /// <summary>
        /// 找到一个合适的连接客户端
        /// </summary>
        /// <returns></returns>
        public async Task<NettyClientState> FindBestAsync()
        {
            double minCount = double.MaxValue;//需要找最小的
            int minTimeIndex = -1;//从头找
            int minCountIndex = -1;
            int firstInitIndex = -1;
            int firstCloseIndex = -1;
          
            int len = nettyClienStates.Length;
            for (int i = 0; i < len; i++)
            {
                var c = nettyClienStates[i];

                if (c.State == Free && c.IsConnected)
                {
                    c.State = Use;
                    return c;//有空闲的直接返回
                }
                else if (c.State == Init)
                {
                    //初始化预留,这样一定有一个预留的客户端连接
                    if (firstInitIndex == -1)
                    {
                        c.Client = new NettyClient();
                        dicState[c.Client] = c;
                        c.Client.DataNotify += Client_DataNotify;
                        await c.Client.Start(c.Address.Host, c.Address.Port);
                        c.State = Open;
                        firstInitIndex = i;
                    }
                }
                else if (c.State == Open)
                {
                    //通知服务端监视线程验证是否无法连接
                    CheckConnect(c);
                }
                else if (c.State == Close)
                {
                    if (firstCloseIndex == -1)
                    {
                        firstCloseIndex = i;
                    }
                }
                else if (c.State == Use)
                {
                    if (minCount > c.CommitRate )
                    {
                        //找到最小包
                        minCount = c.CommitRate;
                        minCountIndex = i;
                    }
                    
                }
                else if (c.State == Free && !c.IsConnected)
                {
                    await c.Client.Close();
                    c.State = Close;
                }
            }
            //
            NettyClientState findC = null;
            if (minCountIndex != -1)
            {
                //数据提交最少的
                findC = nettyClienStates[minCountIndex];
            }
            else if (firstInitIndex != -1)
            {
                //初始化的
                findC = nettyClienStates[firstInitIndex];
            }
            else if (firstCloseIndex != -1)
            {
                findC = nettyClienStates[firstCloseIndex];
                await findC.Client.Start(findC.Address.Host, findC.Address.Port);
                findC.State = Open;
            }
            else if (minTimeIndex != -1)
            {
                //最早一次提交数据的
                findC = nettyClienStates[minTimeIndex];
            }
            return findC;
        }

        /// <summary>
        /// 异步验证客户端连接
        /// </summary>
        /// <param name="clientState"></param>
        private void CheckConnect(NettyClientState clientState)
        {
            bagClients.Add(clientState);
            if (!isCheckClient)
            {
                isCheckClient = true;
                Task.Factory.StartNew(async () =>
                {
                    NettyClientState client = null;
                    while (!bagClients.IsEmpty)
                    {
                        if (bagClients.TryTake(out client))
                        {
                            await ValidateClient(client);
                        }
                    }
                    isCheckClient = false;
                });
            }
        }

        /// <summary>
        /// 验证单个客户端
        /// </summary>
        /// <param name="clientState"></param>
        /// <returns></returns>
        private async Task ValidateClient(NettyClientState clientState)
        {
            bool isAll = false;//是否应该验证所有连接
            if (clientState.Client.IsUnConnect)
            {
                clientState.State = Close;
            }
            if (clientState.State == Open)
            {
                if (clientState.Client.IsUnConnect)
                {
                    Logger.Singleton.WarnFormat("Manager验证客户端连接异常，服务端IP{0},端口{1}", clientState.Address.Host, clientState.Address.Port);
                    await clientState.Client.Close();
                    clientState.State = Close;//关闭的网络是放在最后启动的
                    isAll = true;//网络已经异常，验证所有连接
                }
                else if (clientState.Client.IsConnected)
                {
                    clientState.State = Free;
                }
                else if (ClientSettings.IsConnectTimeout)
                {
                    if ((DateTime.Now.Ticks - clientState.Client.ConnecTime) / MTikcs > ClientSettings.TimeOut)
                    {
                        Logger.Singleton.WarnFormat("客户端连接超时，服务端IP{0},端口{1}", clientState.Address.Host, clientState.Address.Port);
                        if (!clientState.IsConnected)
                        {
                            await clientState.Client.Close();
                            clientState.State = Close;//关闭的网络是放在最后启动的
                        }
                        isAll = true;//网络已经异常，验证所有连接
                    }
                }
            }
            if (isAll)
            {
                await  CheckAll();
            }

        }

        /// <summary>
        /// 验证该服务端所有连接
        /// </summary>
        /// <param name="falge"></param>
        /// <returns></returns>
        private async Task CheckAll()
        {
           
            for (int i = 0; i < nettyClienStates.Length; i++)
            {
                //是否服务端关闭
                var clientState = nettyClienStates[i];

                if (nettyClienStates[i].Client.IsUnConnect)//先验证连接异常
                {
                    nettyClienStates[i].State = Close;//刷新最新状态
                }
                if (clientState.State == Open)
                {
                    if (clientState.Client.IsUnConnect)
                    {
                        Logger.Singleton.WarnFormat("Manager验证客户端连接异常，服务端IP{0},端口{1}", clientState.Address.Host, clientState.Address.Port);
                        await clientState.Client.Close();
                        clientState.State = Close;//关闭的网络是放在最后启动的

                    }
                    else if (clientState.Client.IsConnected)
                    {
                        clientState.State = Free;
                    }
                    else if (ClientSettings.IsConnectTimeout)
                    {
                        if ((DateTime.Now.Ticks - clientState.Client.ConnecTime) / MTikcs > ClientSettings.TimeOut)
                        {
                            Logger.Singleton.WarnFormat("客户端连接超时，服务端IP{0},端口{1}", clientState.Address.Host, clientState.Address.Port);
                            if (!clientState.IsConnected)
                            {
                                await clientState.Client.Close();
                                clientState.State = Close;
                            }

                        }
                    }
                }
            }
           
        }

        /// <summary>
        /// 监测可以关闭的接口
        /// </summary>
        public async Task CheckFree()
        {
            foreach(var item in nettyClienStates)
            {
                if(item.State==Free&&item.LastTick/MTikcs+ShutDownTime<DateTime.Now.Ticks/MTikcs)
                {
                    await item.Client.Close();
                    item.State = Close;
                }
            }
        }

        private void Client_DataNotify(object sender, object msg, string flage)
        {
            var state = dicState[sender as NettyClient];
            state.State = Free;
            if (DataNotify != null)
            {
                DataNotify(this, msg);
            }
        }
    }

    /// <summary>
    /// 客户端封装
    /// </summary>
    public class NettyClientState
    {
        /// <summary>
        /// 客户端
        /// </summary>
        public NettyClient Client { get; set; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 提交数据时间
        /// </summary>
        public long LastTick { get; set; }

        /// <summary>
        /// 创建数据时间
        /// </summary>
        public long CreateTick { get; set; }

        /// <summary>
        /// 客户端地址
        /// </summary>
        public NettyAddress Address { get; set; }

        /// <summary>
        /// 是否已经连续
        /// </summary>
        public bool IsConnected { get { return Client.IsConnected; } }

        /// <summary>
        ///标识连接服务
        /// </summary>
        public string ClientFlage { get; set; }

        /// <summary>
        /// 客户端提交的数据量
        /// </summary>
        public int SubmitCount = 0;

        /// <summary>
        /// 数据提交频率
        /// </summary>
        public double CommitRate
        {
            get { return (double)SubmitCount / DateTime.Now.Ticks - CreateTick; }
        }
    }

}
