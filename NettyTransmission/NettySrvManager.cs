#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCSDB
* 项目描述 ：
* 类 名 称 ：NettySrvManager
* 类 描 述 ：
* 命名空间 ：NetCSDB
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/15 19:04:07
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：NettySrvManager  管理所有服务端及操作绑定
* 使用前要先处理静态字段SrvAddress
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class NettySrvManager
    {
        private static readonly Lazy<NettySrvManager> instance = new Lazy<NettySrvManager>();

        /// <summary>
        /// 所有地址
        /// </summary>
        public static List<NettyAddress> SrvAddress = new List<NettyAddress>();

        public static NettySrvManager Singleton
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// 所有服务端绑定
        /// </summary>
        private List<NettyServer> lstSrv = new List<NettyServer>();

        /// <summary>
        /// 待发送数据
        /// </summary>
        private readonly ConcurrentQueue<byte[]> queue = null;

        /// <summary>
        /// 接收数据
        /// </summary>
        private BlockingCollection<SrvDataSource> block = null;

        /// <summary>
        /// 已经启动的服务端口
        /// </summary>
        private List<int> lstSrvPort = new List<int>();

        /// <summary>
        /// 数据回传事件
        /// </summary>
        public event NettyDataNotify DataNotify;

        public  NettySrvManager()
        {
            NettyAddress address = new NettyAddress()
            {
                Port = 7777
            };
            SrvAddress.Add(address);
        }
       
        /// <summary>
        /// 初始化绑定
        /// 同端口自动排除
        /// </summary>
        public async  void Init()
        {
           
            foreach(var  address in SrvAddress)
            {
                if (lstSrvPort.Contains(address.Port))
                {
                    continue;
                }
                lstSrvPort.Add(address.Port);
                NettyServer server = new NettyServer();
                await server.Start(address.Port, address.Host);
                server.NettySrvFlage = address.Flage;
                lstSrv.Add(server);
                server.DataNotify += Server_DataNotify;
                
            }
        }

        private void Server_DataNotify(object sender, object msg,string flage)
        {
            if (DataNotify != null)
            {
                Console.WriteLine("NettySrvManager推送数据");
                DataNotify(sender, msg,flage);
            }
            else
            {
                SrvDataSource source = new SrvDataSource()
                {
                    Context = sender,
                    Message = msg,
                    Flage=flage

                };
                block.Add(source);
            }
        }

        /// <summary>
        /// 同步获取
        /// </summary>
        /// <returns></returns>
        public SrvDataSource GetData()
        {
            return block.Take();
        }
    }
}
