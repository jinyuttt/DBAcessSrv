/**
* 命名空间: NetSocket 
* 类 名：TCPClient 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：TCPClient 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
   public class TCPClient
    {
        private const int BuffSize =2* 1024*1024;//2M 接收缓存
        private bool connected = false;
        private long entityid = -10000000;
        private ConcurrentDictionary<string, CacheEntity> dic_entity = new ConcurrentDictionary<string, CacheEntity>();
        /// <summary>
        /// 远端
        /// </summary>
        private IPEndPoint endPoint = null;

        // Signals a connection.
        private static AutoResetEvent autoConnectEvent = new AutoResetEvent(false);
        //发送的MySocketEventArgs变量定义.
        private SocketAsyncEventArgsPool lstSendArgs = new SocketAsyncEventArgsPool();

        /// <summary>
        /// 接收数据
        /// </summary>
        private SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs();
        int timeOut = 10000;
        private  Socket socket = null;
        public byte[] HeartServer = Encoding.UTF8.GetBytes("server_heart_~");//服务端发送心跳

        public byte[] HeartClient = Encoding.UTF8.GetBytes("client_heart_~");//客户端的心跳
        public bool EnableHeart;

        /// <summary>  
        /// 接收到客户端的数据事件  
        /// </summary>  
        public event OnReceiveData ReceiveClientData;
       
        
        /// <summary>
        /// 当前连接状态
        /// </summary>
        public bool Connected { get { return socket != null && socket.Connected; } }

      
         public string Host { get; set; }
         
        public int Port { get; set; }

        public string LocalHost { get; set; }

        public int LocalPort { get; set; }
        public DateTime DataTime { get; private set; }

        public TCPClient()
        {
            socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveBufferSize = 64*1024;
            socket.SendBufferSize = 64*1024;
        }
       
        /// <summary>
        /// 连接服务端
        /// 默认超时10s
        /// </summary>
        /// <param name="conTimeOut">连接超时</param>
        /// <returns></returns>
        public SocketError Connect(int conTimeOut=10000)
        {
            if(!string.IsNullOrEmpty(LocalHost)||LocalPort!=0)
            {
                IPEndPoint endPoint = null;
                if (string.IsNullOrEmpty(LocalHost))
                {
                    endPoint = new IPEndPoint(IPAddress.Any, LocalPort);
                }
                else
                {
                    endPoint = new IPEndPoint(IPAddress.Parse(LocalHost),LocalPort);
                }
                socket.Bind(endPoint);
            }
            endPoint = new IPEndPoint(IPAddress.Parse(Host), Port);
            SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
            connectArgs.UserToken = socket;
            connectArgs.RemoteEndPoint =endPoint;
            connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

            socket.ConnectAsync(connectArgs);
            if (!autoConnectEvent.WaitOne(timeOut))
            {
                //阻塞. 让程序在这里等待,直到连接响应后再返回连接结果
                socket.Close();
                socket.Dispose();
            }
         
            return connectArgs.SocketError;
        }

        internal void Disconnect()
        {
            socket.Disconnect(false);
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="e">连接成功时</param>
        private void initArgs(SocketAsyncEventArgs e)
        {
          
          
            //接收参数
            receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            receiveEventArgs.UserToken = e.UserToken;
            receiveEventArgs.SetBuffer(new byte[BuffSize], 0, BuffSize);

            //启动接收,不管有没有,一定得启动.否则有数据来了也不知道.
            if (!e.ConnectSocket.ReceiveAsync(receiveEventArgs))
                ProcessReceive(receiveEventArgs);
        }

       
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
        
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    {
                        lstSendArgs.Push(e);
                        ProcessSend(e);
                        if(e.UserToken.Equals("a")||e.UserToken.Equals("b"))
                        {
                            //不做
                        }
                        else
                        {
                            CacheEntity entity = null;
                            if(dic_entity.TryRemove(e.UserToken.ToString(),out entity))
                            {
                                entity.Dispose();
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of connection.
            autoConnectEvent.Set(); //释放阻塞.
            // Set the flag for socket connected.
            connected = (e.SocketError == SocketError.Success);
            //如果连接成功,则初始化socketAsyncEventArgs
            if (connected)
            {
                try
                {
                    IPEndPoint iPEnd = e.ConnectSocket.LocalEndPoint as IPEndPoint;
                    LocalHost = iPEnd.Address.ToString();
                    LocalPort = iPEnd.Port;
                }
                catch
                {

                }
                initArgs(e);
            }
            else
            {
                e.Dispose();
            }
        }

        /// <summary>
        /// 接收
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                // check if the remote host closed the connection
                Socket token = (Socket)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //读取数据
                    byte[] data = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, data, 0, e.BytesTransferred);
                    //
                    if (EnableHeart && data.Length == HeartServer.Length)
                    {
                        if (Enumerable.SequenceEqual(data, HeartServer))
                        {
                            //收到心跳
                            Console.WriteLine("收到客户端心跳");
                            token.Send(HeartClient);
                            DataTime = DateTime.Now;
                            if (!token.ReceiveAsync(e))
                                this.ProcessReceive(e);
                            return;
                        }
                    }
                    DoReceiveEvent(data);
                    if (!token.ReceiveAsync(e))
                        this.ProcessReceive(e);
                }
                else
                {
                    ProcessError(e);
                }
            }
            catch (Exception xe)
            {
                Console.WriteLine(xe.Message);
            }
        }

        /// <summary>
        /// 发送
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                ProcessError(e);
            }
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        /// <param name="e"></param>
        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = (Socket)e.UserToken;
            if (s.Connected)
            {
                // close the socket associated with the client
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    // throws if client process has already closed
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                    connected = false;
                }
            }
            //这里一定要记得把事件移走,如果不移走,当断开服务器后再次连接上,会造成多次事件触发.
            lstSendArgs.Clear();
            receiveEventArgs.Completed -= IO_Completed;

            //if (ServerStopEvent != null)
            //    ServerStopEvent();
        }

        
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data,string flage="a")
        {
            SendBuffer(data, 0, data.Length, flage);
        }

        /// <summary>
        /// 发送数组
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        public void SendBuffer(byte[]buf,int offset=0,int len=0,string flage="b")
        {
            if (connected)
            {
                len = len == 0 ? buf.Length : len;
                //查找有没有空闲的发送SocketEventArgs,有就直接拿来用,没有就创建新的.So easy!
                SocketAsyncEventArgs sendArgs = lstSendArgs.Pop();
                if (sendArgs.UserToken == null)
                {
                    //说明是新的
                    sendArgs.Completed += this.IO_Completed;
                    sendArgs.UserToken =flage;
                }

                //客户端发送太零散，已经有byte[]就直接用
                //不通过缓存了
                sendArgs.SetBuffer(buf,offset,len);
                if(socket.SendAsync(sendArgs))
                {
                    ProcessSend(sendArgs);
                }
            }
            else
            {
                throw new SocketException((Int32)SocketError.NotConnected);
            }
        }


        /// <summary>
        /// 通过缓存发送数据
        /// </summary>
        /// <param name="entity"></param>
        public void SendEntity(CacheEntity entity,string flage="c")
        {
            string id = Interlocked.Increment(ref entityid).ToString();
           
            dic_entity[id] = entity;
            if(entity.Fix==0)
            {
                SendBuffer(entity.Buffer, entity.Offset, entity.Length,id);
            }
            else
            {
                Send(entity.Buffer,id);
            }
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        public void SendHeart()
        {
           if(socket.Connected)
            {
                socket.Send(HeartClient);
            }
        }
        /// <summary>
        /// 处理数据
        /// </summary>
        /// <param name="buff"></param>
        private void DoReceiveEvent(byte[] buff)
        {

            if (ReceiveClientData != null)
            {
                Task.Factory.StartNew(() =>
            {

                AsyncTcpUserToken token = new AsyncTcpUserToken();
                if (string.IsNullOrEmpty(LocalHost))
                {
                    IPEndPoint point = socket.LocalEndPoint as IPEndPoint;
                    LocalHost = point.Address.ToString();
                }
                token.IPAddress = IPAddress.Parse(LocalHost);
                token.Remote = endPoint;
                token.Socket = socket;
                token.Client = this;
                ReceiveClientData(token, buff);
            });
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Dispose()
        {
            autoConnectEvent.Close();
            if (socket.Connected)
            {
                socket.Close();
               
            }
            socket.Dispose();
            receiveEventArgs.Dispose();
        }
    }
}
