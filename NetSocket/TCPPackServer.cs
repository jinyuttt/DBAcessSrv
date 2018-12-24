/**
* 命名空间: NetSocket 
* 类 名：TCPServerPack 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：TCPServerPack 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class TCPPackServer: IServer
    {

        private TCPServer server = null;

        private ConcurrentDictionary<long, AsyncTcpUserToken> dic_Client = null;

        private long clientID = 0;

        private DateTime lastTime = DateTime.Now;

        /// <summary>
        /// 本机IP
        /// </summary>
        public string Host { get; set; }


        /// <summary>
        /// 本机端口
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 同时监视客户端数量
        /// 默认100；
        /// </summary>
        public int MaxClientNum { get; set; }

        /// <summary>
        /// 服务端缓存大小
        /// 尽可能大（M或者个数）
        /// 默认1024
        /// </summary>
        public  int TotalSize { get; set; }

        /// <summary>
        /// 每个缓存大小
        /// 默认2048字节
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// 服务端换模型
        ///默认固定大小缓存，开辟一个大的byte[]
        /// </summary>
        public bool IsFixCacheSize { get; set; }

        /// <summary>
        /// 是否开启心跳
        /// </summary>
        public bool IsEnableHeart { get; set; }

        /// <summary>
        /// 心跳时间
        /// 该时间内没有数据收发心跳
        /// 秒
        /// </summary>
        public int HeartTime { get; set; }

        /// <summary>
        /// 所有客户端发送一次心跳时间
        /// 分钟
        /// </summary>
        public int AllHeartTime { get; set; }
        

        #region 定义事件  
        /// <summary>  
        /// 客户端连接数量变化事件  
        /// </summary>  
        public event OnClientNumberChange ClientNumberChange;

        /// <summary>  
        /// 接收到客户端的数据事件  
        /// </summary>  
        public event OnReceiveData ReceiveClientData;


        #endregion

        public TCPPackServer()
        {
            MaxClientNum = 100;
            TotalSize = 1024;
            BufferSize = 2048;
            IsFixCacheSize = true;
            IsEnableHeart = true;
            HeartTime = 30;
            AllHeartTime =10;
            dic_Client = new ConcurrentDictionary<long, AsyncTcpUserToken>();


        }

        /// <summary>
        /// 启动
        /// </summary>
        /// <returns></returns>
       public bool Start()
        {
            if(server!=null)
            {
                server.Stop();
                server.ReceiveClientData -= Server_ReceiveClientData;
                server.ClientNumberChange -= Server_ClientNumberChange;
            }
            server = new TCPServer(MaxClientNum, TotalSize, BufferSize);
            server.ReceiveClientData += Server_ReceiveClientData;
            server.ClientNumberChange += Server_ClientNumberChange;
            if(server.Start(Port, Host))
            {
                StartHeart();
                return true;
            }
            return false;
        }

        private void Server_ClientNumberChange(int num, AsyncTcpUserToken token)
        {

            if (num > 0)
            {
                token.DataTime = DateTime.Now;
                if (token.TokenID == 0)
                {
                    token.TokenID = Interlocked.Increment(ref clientID);
                    dic_Client[token.TokenID] = token;
                }
            }
            else
            {
                AsyncTcpUserToken tmp = null;
                dic_Client.TryRemove(token.TokenID, out tmp);
            }
              if(ClientNumberChange!=null)
            {
                ClientNumberChange(num, token);
            }
        }

        private void Server_ReceiveClientData(AsyncTcpUserToken token, byte[] buff)
        {
            if (token.TokenID == 0)
            {
                token.TokenID = Interlocked.Increment(ref clientID);
                token.DataTime = DateTime.Now;
                dic_Client[token.TokenID] = token;
            }
            if(ReceiveClientData!=null)
            {
                ReceiveClientData(token, buff);
            }
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            server.Stop();
        }

        /// <summary>
        /// 心跳
        /// </summary>
       private void StartHeart()
        {
            
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(HeartTime * 1000);
                if (IsEnableHeart)
                {
                    if ((DateTime.Now - lastTime).TotalMinutes > AllHeartTime)
                    {
                        server.SendHeart();
                        lastTime = DateTime.Now;

                    }
                    else
                    {
                        foreach (var kv in dic_Client)
                        {
                            if ((DateTime.Now - kv.Value.DataTime).TotalSeconds > HeartTime)
                            {
                                server.SendHeart(kv.Value);
                            }
                        }
                    }
                }
                StartHeart();//重复
            });
        }
       
        /// <summary>
        /// 关闭客户端
        /// </summary>
        /// <param name="socket"></param>
        public void CloseClient(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }
        }


        /// <summary>
        /// 关闭客户端
        /// </summary>
        /// <param name="token"></param>
        public void CloseClient(AsyncTcpUserToken token)
        {
            server.CloseClient(token);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="message"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <param name="isCache"></param>
        public void SendPackage(AsyncTcpUserToken token, byte[] message, int offset, int len, int isCache = -1)
        {
            int index = 0;
            byte[] tmp = null;
            CacheEntity entity = null;
           switch(isCache)
            {
                case -1:
                    server.SendPackage(token, message, offset, len);
                    break;
                case 0:
                    {
                        do
                        {
                            tmp = DataPack.PackTCP(message, ref index);
                            server.SendMessage(token,message);
                        } while (tmp != null && index < message.Length);
                    }
                    break;
                case 1:
                    {
                        do
                        {
                             entity = DataPack.PackEntityTCP(message, ref index);
                            server.SendData(token, entity);
                        } while (entity != null && index < message.Length);
                    }
                    break;
                case 2:
                    {
                        do
                        {
                            entity = DataPack.PackEntityTCP(message, ref index);
                            server.SendData(token, entity,"srv");
                        } while (entity != null && index < message.Length);
                    }
                    break;
            }
        }
    }
}
