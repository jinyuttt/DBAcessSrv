/**
* 命名空间: NetSocket 
* 类 名：TCPClientPack 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：TCPClientPack 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class TCPPackClient
    {

        TCPClient client = null;


        List<byte> lst = null;//收集数据
        private int currentSize = -1;//当前包大小
        private AsyncTcpUserToken asyncUser = null;
        private int head = 4;
        private int maxSize = 5 * 1024 * 1024;//5M

        /// <summary>
        /// 远端IP
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 远端端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 本地IP
        /// </summary>
        public string LocalHost { get; set; }

        /// <summary>
        /// 本地端口
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// 心跳时间；超过该时间没有收发数据则发送心跳(秒)
        /// IsClientHeart=true;
        /// 默认30s
        /// 
        /// </summary>
        public int HeartTime { get; set; }

        /// <summary>
        /// 心跳验证
        /// </summary>
        public bool IsEnableHeart { get { return client.EnableHeart; } set { client.EnableHeart = value; } }

        /// <summary>
        /// 客户端启动定时心跳验证
        /// </summary>
        public bool IsClientHeart { get; set; }


        /// <summary>  
        /// 接收到客户端的数据事件  
        /// </summary>  
        public event OnReceiveData ReceiveClientData;

        public BlockingCollection<AsyncTcpUserToken> queue = null;
        private bool isPack = true;


        /// <summary>
        /// 接收数据是否打包
        /// 默认true
        /// </summary>
        public bool IsPack { get { return IsPack; } set { isPack = value; } }

        public bool Connected {
            get {
                   if (client == null)
                       { return false; }
                     else {return client.Connected; }
            }
        }

        /// <summary>
        /// 最大的数据量
        /// </summary>
        public int MaxSize { get { return maxSize; } set { maxSize = value;lst.Capacity = maxSize;lst.TrimExcess(); } }

        public TCPPackClient()
        {
            queue = new BlockingCollection<AsyncTcpUserToken>();
            lst = new List<byte>(maxSize);
            IsEnableHeart = false;
            HeartTime = 30;

        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public bool Connect(int timeOut = 10000)
        {
            if (client != null)
            {
                client.Disconnect();
                client.Dispose();
            }
            client = new TCPClient();
            client.LocalHost = LocalHost;
            client.LocalPort = LocalPort;
            client.Host = Host;
            client.Port = Port;
            if(client.Connect(timeOut)== SocketError.Success)
            {
                client.ReceiveClientData += Client_ReceiveClientData;
                StartHeart();
                LocalHost = client.LocalHost;
                LocalPort = client.LocalPort;
                return true;
            }
            return false;
        }

        private void Client_ReceiveClientData(AsyncTcpUserToken token, byte[] buff)
        {
            asyncUser = token;
            //收集数据
            lock (lst)
            {
               
                if (isPack)
                {
                    lst.AddRange(buff);
                    if (currentSize < 0 && lst.Count > 4)
                    {
                        currentSize = BitConverter.ToInt32(lst.GetRange(0, 4).ToArray(), 0);
                        head = 4;
                    }
                    if (lst.Count > currentSize)
                    {
                        //说明数据收集完成
                        byte[] tmp = new byte[currentSize];
                        lst.CopyTo(head, tmp, 0, tmp.Length);//去除头
                        DoEventClient(tmp);
                        lst.RemoveRange(0, currentSize + head);//包括头
                        if (lst.Count > 4)
                        {
                            currentSize = BitConverter.ToInt32(lst.GetRange(0, 4).ToArray(), 0);
                            head = 4;
                        }
                        else
                        {
                            currentSize = -1;
                        }
                    }
                    else if (lst.Count > maxSize)
                    {
                        //byte[] tmp= lst.ToArray();
                        //DoEventClient(tmp);
                        //lst.Clear();
                        //currentSize -= lst.Count;
                        byte[] tmp = new byte[maxSize];
                        lst.CopyTo(head, tmp, 0, tmp.Length);//去除头
                        DoEventClient(tmp);
                        currentSize = currentSize - (maxSize + head);
                        head = 0;
                    }
                }
                else
                {
                    DoEventClient(buff);
                }

            }

        }
        private void DoEventClient(byte[]data)
        {
            if(ReceiveClientData!=null)
            {
                Task.Factory.StartNew(() =>
                {
                    AsyncTcpUserToken token = new AsyncTcpUserToken();
                    token.Remote = asyncUser.Remote;
                    token.IPAddress = asyncUser.IPAddress;
                    
                    ReceiveClientData(token, data);
                });
            }
            else
            {
                AsyncTcpUserToken token = null;
               if(queue.Count>1000)
                {
                    queue.TryTake(out token);
                }
                token = new AsyncTcpUserToken();
                token.Remote = asyncUser.Remote;
                token.IPAddress = asyncUser.IPAddress;
                token.Data = data;
                queue.TryAdd(token);
            }
        }
       
        /// <summary>
        /// 启动心跳
        /// </summary>
        private void StartHeart()
        {
           
                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(HeartTime * 1000);
                    if (IsClientHeart)
                    {
                        if ((DateTime.Now - client.DataTime).TotalSeconds > HeartTime)
                        {
                            client.SendHeart();
                        }
                    }
                    if (client.Connected)
                    {
                        StartHeart();//重复
                    }
                });
           
        }


        /// <summary>
        /// 供外部统一管理
        /// </summary>
        public void SendHeart()
        {
            client.SendHeart();
        }

        #region 发送

        /// <summary>
        /// 直接发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        public void SendData(byte[]data,int offset,int len=0)
        {
            if (len == 0 && offset == 0)
            {
                client.Send(data);
            }
            else
            {
                client.SendBuffer(data, offset, len);
            }
        }


       /// <summary>
       /// 打包发送
       /// </summary>
       /// <param name="data"></param>
        public void SendPack(byte[]data)
        {
            int index = 0;
            byte[] sendBuf = null;
            do
            {
                sendBuf = DataPack.PackTCP(data, ref index);
                client.SendBuffer(sendBuf, 0, sendBuf.Length);
            } while (sendBuf.Length > 0 && index < data.Length);
          
        }

        /// <summary>
        /// 使用缓存打包数据
        /// </summary>
        /// <param name="data"></param>
        public void SendBufferPack(byte[] data)
        {
            int index = 0;
            CacheEntity entity = null;
            do
            {
                 entity = DataPack.PackEntityTCP(data, ref index);
                 client.SendEntity(entity);
            } while (entity!=null&&index<data.Length);

        }
        #endregion

        public void Close()
        {
            client.ReceiveClientData -= Client_ReceiveClientData;
            client.Disconnect();
            client.Dispose();
            client = null;
            queue = null;
            lst.Clear();
            lst = null;
        }

    }
}
