/**
* 命名空间: NetSocket 
* 类 名：TCPServer 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace NetSocket
{
    #region 定义委托  

    /// <summary>  
    /// 客户端连接数量变化时触发  
    /// </summary>  
    /// <param name="num">当前增加客户的个数(用户退出时为负数,增加时为正数,一般为1)</param>  
    /// <param name="token">增加用户的信息</param>  
    public delegate void OnClientNumberChange(int num, AsyncTcpUserToken token);

    /// <summary>  
    /// 接收到客户端的数据  
    /// </summary>  
    /// <param name="token">客户端</param>  
    /// <param name="buff">客户端数据</param>  
    public delegate void OnReceiveData(AsyncTcpUserToken token, byte[] buff);

    #endregion
    /// <summary>
    /// 功能描述    ：TCPServer  接收连接，所有服务端缓存采用单例共用，设置固定缓存大小区
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class TCPServer: IServer
    {
        private Socket listenSocket;
        private int m_maxConnectNum;    //最大连接数  
        private int m_revBufferSize;    //接收缓存大小
        private BufferManager m_bufferManager; //服务端缓存
                //监听Socket  
        private SocketAsyncEventArgsPool m_pool;
        private int m_clientCount;              //连接的客户端数量  

        private List<AsyncTcpUserToken> m_clients; //客户端列表  
        private long entityid = -100000;//缓存标记
        private ConcurrentDictionary<string, CacheEntity> dic_Entity = null;//保持客户端缓存进行释放
        private int totalBufSize = 0;

        public byte[] HeartServer = Encoding.UTF8.GetBytes("server_heart_~");//服务端发送心跳

        public byte[] HeartClient = Encoding.UTF8.GetBytes("client_heart_~");//客户端的心跳

        /// <summary>
        /// 启用心跳
        /// 只有启用才会判断客户端心跳自动回避并且返回一次服务端心跳，否则需要外部程序自己判断心跳
        /// 只有启用才可以在服务端发送心跳，心跳发送任然需要外部控制
        /// 默认启用
        /// </summary>
        public bool EnableHeart { get; set; }

        #region 定义事件  
        /// <summary>  
        /// 客户端连接数量变化事件  
        /// </summary>  
        public event OnClientNumberChange ClientNumberChange;


        /// <summary>  
        /// 接收到客户端的数据事件  
        /// </summary>  
        public event OnReceiveData ReceiveClientData;

        private const int MByte = 1024 * 1024;
        #endregion

        /// <summary>
        /// 使用固定大小缓存的方式
        /// 另外一种是动态生成
        /// </summary>
        public bool IsFixCacheSize { get; set; }
        public TCPServer(int numConnections=100,int totalMSize=1024, int receiveBufferSize= 2*1460)
        {
            m_clientCount = 0;
            m_maxConnectNum = numConnections;
            m_revBufferSize = receiveBufferSize;

            m_bufferManager = BufferManager.instance;
            m_bufferManager.BufferSize = receiveBufferSize;
            m_bufferManager.Capacity = totalMSize* MByte;
            m_pool = new SocketAsyncEventArgsPool();
            //m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
            dic_Entity = new ConcurrentDictionary<string, CacheEntity>();
            IsFixCacheSize = true;
            totalBufSize = totalMSize;
        }


        /// <summary>  
        /// 初始化  
        /// </summary>  

        private void Init()
        {
            if(!IsFixCacheSize)
            {
                m_bufferManager.MaxBufferCount = totalBufSize * 1024 * 1024 * 1024;//可以使用最多缓存
            }
            m_clients = new List<AsyncTcpUserToken>();
           
            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_maxConnectNum; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                AsyncTcpUserToken userToken = new AsyncTcpUserToken();
                userToken.Server = this;
                readWriteEventArg.UserToken = userToken;

                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                if (IsFixCacheSize)
                {
                    m_bufferManager.SetBuffer(readWriteEventArg);
                }
                else
                {
                    m_bufferManager.GetBuffer(readWriteEventArg);
                }
                // add SocketAsyncEventArg to the pool  
                m_pool.Push(readWriteEventArg);
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="port"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public bool Start(int port,string host=null)
        {
            try
            {
                Init();
                m_clients.Clear();
                IPEndPoint localEndPoint = null;
                if (!string.IsNullOrEmpty(host))
                {
                  localEndPoint=  new IPEndPoint(IPAddress.Parse(host), port);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, port);
                }
                listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.SendBufferSize = 64 * 1024;
                listenSocket.ReceiveBufferSize = 64 * 1024;
                listenSocket.Bind(localEndPoint);
                // start the server with a listen backlog of 100 connections  
                listenSocket.Listen(m_maxConnectNum);
                // post accepts on the listening socket  
                StartAccept(null);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
       
        /// <summary>  
        /// 停止服务  
        /// </summary>  
        public void Stop()
        {
            foreach (AsyncTcpUserToken token in m_clients)
            {
                try
                {
                    token.Socket.Shutdown(SocketShutdown.Both);
                }
                catch (Exception) { }
            }
            try
            {
                listenSocket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }

            listenSocket.Close();
            int c_count = m_clients.Count;
            lock (m_clients) { m_clients.Clear(); }

            //if (ClientNumberChange != null)
            //    ClientNumberChange(-c_count, null);
        }

        /// <summary>
        /// 关闭客户端，此时任然在接收数据
        /// 所以CloseClientSocket会被调用
        /// </summary>
        /// <param name="token"></param>
        public void CloseClient(AsyncTcpUserToken token)
        {
            try
            {
                token.Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception) { }
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused  
                acceptEventArg.AcceptSocket = null;
            }
          
             //m_maxNumberAcceptedClients.WaitOne();
            //说明已经有接收的连接了，再次接收连接
            if (!listenSocket.AcceptAsync(acceptEventArg))
            {
                //已经获取连接，立即处理接收的连接
                ProcessAccept(acceptEventArg);
            }
        }

        /// <summary>
        /// 处理连接
        /// </summary>
        /// <param name="acceptEventArg"></param>
        private void ProcessAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                Interlocked.Increment(ref m_clientCount); 
                SocketAsyncEventArgs readEventArgs = m_pool.Pop();
                AsyncTcpUserToken userToken = (AsyncTcpUserToken)readEventArgs.UserToken;
                if(userToken==null)
                {
                    //说明是新的，已经使用的读写还没有返回
                    userToken = new AsyncTcpUserToken();
                    readEventArgs.UserToken = userToken;
                    readEventArgs.Completed += IO_Completed;
                    userToken.Server = this;
                    if (IsFixCacheSize) //分配缓存
                    {
                        m_bufferManager.SetBuffer(readEventArgs);
                    }
                    else
                    {
                        m_bufferManager.GetBuffer(readEventArgs);
                    }
                   
                }
                userToken.Socket = acceptEventArg.AcceptSocket;
                userToken.ConnectTime = DateTime.Now;
                userToken.Remote = acceptEventArg.AcceptSocket.RemoteEndPoint;
                userToken.IPAddress = ((IPEndPoint)(acceptEventArg.AcceptSocket.RemoteEndPoint)).Address;

                lock (m_clients) { m_clients.Add(userToken); }

                if(ClientNumberChange!=null)
                {
                    //防止同步，不返回，无法绑定数据接收
                    Task.Factory.StartNew(() =>
                    {
                        ClientNumberChange(1, userToken);
                    });
                  
                }
                //获取到连接后立即准备接收数据；绑定接收事件
                if (!acceptEventArg.AcceptSocket.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(readEventArgs);
                }
            }
            catch (Exception ex)
            {
                //RuncomLib.Log.LogUtils.Info(me.Message + "\r\n" + me.StackTrace);
            }
            if (acceptEventArg.SocketError == SocketError.OperationAborted) return;
            StartAccept(acceptEventArg);

        }


        /// <summary>
        /// 处理接收
        /// </summary>
        /// <param name="readEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs readEventArgs)
        {
            
                try
                {
                    // check if the remote host closed the connection  
                    AsyncTcpUserToken token = (AsyncTcpUserToken)readEventArgs.UserToken;
                    if (readEventArgs.BytesTransferred > 0 && readEventArgs.SocketError == SocketError.Success)
                    {
                        //读取数据;这里也可以考虑建立缓存池获取
                        byte[] data = new byte[readEventArgs.BytesTransferred];
                        Array.Copy(readEventArgs.Buffer, readEventArgs.Offset, data, 0, readEventArgs.BytesTransferred);
                        //lock (token.Buffer)
                        //{
                        //    token.Buffer.AddRange(data);
                        //}
                       
                    //考虑道客户端数据大小，所以接收到一个缓存大小数据
                    //交给后端处理；socket作为基础操作，只管接收
                    if(EnableHeart&&data.Length==HeartClient.Length)
                    {
                        if (Enumerable.SequenceEqual(data, HeartClient))
                        {
                            //收到心跳
                            Console.WriteLine("收到客户端心跳");
                            token.Socket.Send(HeartServer);
                            token.DataTime = DateTime.Now;
                            if (!token.Socket.ReceiveAsync(readEventArgs))
                                this.ProcessReceive(readEventArgs);
                            return;
                        }
                    }
                    if (ReceiveClientData != null)
                          ReceiveClientData(token, data);
                    //继续接收下一次客户端发送的数据
                    //也可能是客户端发送过大，一个buffer接收不了
                    if (!token.Socket.ReceiveAsync(readEventArgs))
                            this.ProcessReceive(readEventArgs);
                    }
                    else
                    {
                        CloseClientSocket(readEventArgs);
                    }
                }
                catch (Exception xe)
                {
                    //RuncomLib.Log.LogUtils.Info(xe.Message + "\r\n" + xe.StackTrace);
                }
            
        }

        /// <summary>
        /// 处理发送完成后的信息
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            //TCP这里不好判断，缓存全部回收
            if (e.SocketError == SocketError.Success)
            {
                // done echoing data back to the client  
                //AsyncUserToken token = (AsyncUserToken)e.UserToken;
                // read the next block of data send from the client  
                //bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                //if (!willRaiseEvent)
                //{
                //    ProcessReceive(e);
                //}
                //发送完成回收
                AsyncTcpUserToken token = e.UserToken as AsyncTcpUserToken;
                if (token!=null&&!string.IsNullOrEmpty(token.UserInfo))
                {
                    CacheEntity entity = null;
                    if(dic_Entity.TryRemove(token.UserInfo,out entity))
                    {
                        //客户端发送使用的缓存要释放
                        entity.Dispose();
                    }
                    e.UserToken = null;
                    e.SetBuffer(null, 0, 0);
                    e.Dispose();//新建的，必须释放,不是来自缓存
                    return;
                }
                if (e.Count < m_bufferManager.BufferSize)
                {
                    //直接释放内存,不是来自缓存
                    e.SetBuffer(null, 0, 0);
                    e.UserToken = null;
                    m_pool.Push(e);
                }
                else
                {
                    //缓存回收，发送的需要回收，接收一定循环，发送不是
                    if (IsFixCacheSize)
                    {
                        m_bufferManager.FreeBuffer(e);
                    }
                    else
                    {
                        m_bufferManager.FreePoolBuffer(e);
                    }
                    m_pool.Push(e);
                }
               
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        
       /// <summary>
       /// 关闭客户端通信
       /// </summary>
       /// <param name="e"></param>
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncTcpUserToken token = e.UserToken as AsyncTcpUserToken;
            lock (m_clients) { m_clients.Remove(token); }
            //如果有事件,则调用事件,发送客户端数量变化通知  
            if (ClientNumberChange != null)
                  ClientNumberChange(-1, token);
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) { }
            token.Socket.Close();
            Interlocked.Decrement(ref m_clientCount);
            if (token != null && !string.IsNullOrEmpty(token.UserInfo))
            {
                CacheEntity entity = null;
                if (dic_Entity.TryRemove(token.UserInfo, out entity))
                {
                    //客户端发送使用的缓存要释放
                    entity.Dispose();
                }
                e.UserToken = null;
                e.SetBuffer(null, 0, 0);
                e.Dispose();//新建的，必须释放,不是来自缓存
                return;
            }
            //接收的一定是缓存的
            e.UserToken = new AsyncTcpUserToken();
            m_pool.Push(e);
        }

        /// <summary>
        /// 连接接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler  
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }
      

        /// <summary>
        /// 发送心跳
        /// </summary>
         public void SendHeart()
        {
            List<AsyncTcpUserToken> remove = new List<AsyncTcpUserToken>();
            foreach (AsyncTcpUserToken token in m_clients)
            {
                if (token.Socket.Connected)
                {
                    try
                    {
                        token.Socket.Send(HeartServer);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        remove.Add(token);
                    }
                }
                else
                {
                    remove.Add(token);
                }
            }
            if (remove.Count > 0)
            {
                lock (m_clients)
                {
                    foreach (AsyncTcpUserToken token in remove)
                    {
                        m_clients.Remove(token);
                        CloseClient(token);
                    }
                }
            }
        }
         
        /// <summary>
        /// 发送心跳
        /// </summary>
        /// <param name="token"></param>
        public  void SendHeart(AsyncTcpUserToken token)
        {
            bool isSucess = false;
            if (token.Socket.Connected)
            {
                try
                {
                    token.Socket.Send(HeartServer);
                    isSucess = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    
                }
            }
            if(!isSucess)
            {
                CloseClient(token);
            }
           
        }


        /// <summary>  
        /// 对数据按照服务端缓存分包
        /// 然后发送
        /// </summary>  
        /// <param name="token"></param>  
        /// <param name="message"></param>  
        /// <returns></returns>  
        public void SendMessage(AsyncTcpUserToken token, byte[] message)
        {
            if (token == null || token.Socket == null || !token.Socket.Connected)
                return;
            try
            {

                //token.Socket.Send(buff);  //这句也可以发送, 可根据自己的需要来选择  
                //新建异步发送对象, 发送消息
                int index = 0;
                int len = 0;
                do
                {
                    SocketAsyncEventArgs sendArg = m_pool.Pop();
                    if (null == sendArg.UserToken)
                    {
                        //说明是新建
                        sendArg.Completed += this.IO_Completed;
                        //m_bufferManager.SetBuffer(sendArg);
                        if (IsFixCacheSize) //分配缓存
                        {
                            m_bufferManager.SetBuffer(sendArg);
                        }
                        else
                        {
                            m_bufferManager.GetBuffer(sendArg);
                        }

                    }
                    sendArg.UserToken = token;
                    byte[] buf = sendArg.Buffer;
                    int curLen = message.Length - index;
                    if (curLen < sendArg.Count)
                    {
                        //重新开辟缓存，原缓存不合适
                        if(sendArg.Buffer!=null)
                        {
                            if (IsFixCacheSize) //分配缓存
                            {
                                m_bufferManager.FreeBuffer(sendArg);
                            }
                            else
                            {
                                m_bufferManager.FreePoolBuffer(sendArg);
                            }
                        }
                        sendArg.SetBuffer(new byte[curLen], 0, curLen);
                        Array.Copy(message, index, sendArg.Buffer, sendArg.Offset, curLen);
                        index += curLen;
                        if (token.Socket.SendAsync(sendArg))
                        {
                            ProcessSend(sendArg);
                        }
                       
                    }
                    else
                    {
                        len = sendArg.Count;
                        Array.Copy(message, index, buf, sendArg.Offset, len);
                        index += len;
                        if(token.Socket.SendAsync(sendArg))
                        {
                            ProcessSend(sendArg);
                        }
                    }
                    
                } while (index >= message.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }


        /// <summary>
        /// 发送数据;从缓存中来的数据
        /// </summary>
        /// <param name="token">客户端连接</param>
        /// <param name="entity">发送的缓存实体</param>
        /// <param name="flage">标记，默认null表示发送继续使用客户端缓存发送，否则由服务端分包使用服务端缓存发送</param>
        public void SendData(AsyncTcpUserToken token,CacheEntity entity,string flage=null)
        {
            if(string.IsNullOrEmpty(flage))
            {
                //继续使用客户端缓存发送，不能分包使用服务端缓存
                SocketAsyncEventArgs sendArg = new SocketAsyncEventArgs();
                sendArg.Completed += this.IO_Completed;
                sendArg.SetBuffer(entity.Buffer, entity.Offset, entity.Length);
                token.UserInfo = Interlocked.Increment(ref entityid).ToString();
                dic_Entity[token.UserInfo] = entity;
                sendArg.UserToken = token;
                if(token.Socket.SendAsync(sendArg))
                {
                    ProcessSend(sendArg);
                }
            }
            else
            {
                SendMessage(token, entity.Buffer);
                entity.Dispose();
            }
        }

        /// <summary>
        /// 这里不会分包，交给底层
        /// 无法判断是缓存
        /// </summary>
        /// <param name="token"></param>
        /// <param name="message"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        /// <param name="isCache"></param>
        public void SendPackage(AsyncTcpUserToken token, byte[] message, int offset, int len, int isCache = 0)
        {
             if(len==0)
            {
                SendMessage(token, message);
            }
             else
            {
                byte[] tmp = new byte[len];
                Array.Copy(message, offset, tmp,0, len);
                SendMessage(token, tmp);
            }
        }
    }
}
