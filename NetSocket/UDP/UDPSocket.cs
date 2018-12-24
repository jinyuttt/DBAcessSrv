/**
* 命名空间: NetSocket 
* 类 名：UDPSocket 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSocket
{

    public delegate void OnReceiveUdpData(object sender, AsyncUdpUserToken token);
    /// <summary>
    /// 功能描述    ：UDPSocket UDP通信，所有属性Bind之前设置
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class UDPSocket: IUDP
    {
        private Socket socket;

        private SocketAsyncEventArgs receiveSocketArgs;

        private IPEndPoint localEndPoint;

        private byte[] receivebuffer;

        private BufferManager bufferManager = null;

        private SocketAsyncEventArgsPool pool = null;

        private CacheManager cacheManager = null;

        private UserTokenPool tokenPool = null;

        public byte[] HeartBytes = Encoding.UTF8.GetBytes("udp_heart_~");//客户端的心跳

        private Dictionary<string, AsyncUdpUserToken> dicToken = new Dictionary<string, AsyncUdpUserToken>();

        public bool EnableHeart { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 默认65535
        /// bind之前设置
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// 缓存区间最大大小(M或者个数)
        /// 默认1024
        /// 这里是2个相同的缓存区；合理设置
        /// </summary>
        public int TotalBufSize { get; set; }

        /// <summary>
        /// 默认false
        /// UDP发送零散
        /// Bind之前设置
        /// </summary>
        public bool IsFixCache { get; set; }

        public event OnReceiveUdpData OnDataReceived;
        private const int MByte =1024 * 1024;

        public int TokenMaxLeftNum { get; set; }

        public UDPSocket()
        {
            BufferSize = 65535;
            TotalBufSize = 1024*MByte;
            IsFixCache = false;
            pool = new SocketAsyncEventArgsPool();
            bufferManager = new BufferManager();
            cacheManager = new CacheManager();
            tokenPool = new UserTokenPool();
            TokenMaxLeftNum = Environment.ProcessorCount*10;
            if(TokenMaxLeftNum == 0)
            {
                TokenMaxLeftNum = 100;
            }
        }


        public void Bind()
        {
            //
            bufferManager.BufferSize = BufferSize;
            cacheManager.BufferSize = BufferSize;
            bufferManager.Capacity = TotalBufSize*MByte;
            cacheManager.Capacity = TotalBufSize*MByte;
            tokenPool.MaxLeftNum = TokenMaxLeftNum;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SendBufferSize = 64 * 1024;
            socket.ReceiveBufferSize = 64 * 1024;
            if (string.IsNullOrEmpty(Host))
            {
                localEndPoint = new IPEndPoint(IPAddress.Any, Port);
            }
            else
            {
                localEndPoint = new IPEndPoint(IPAddress.Parse(Host), Port);
            }
            socket.Bind(localEndPoint);
            receivebuffer = new byte[BufferSize];
            receiveSocketArgs = new SocketAsyncEventArgs();
            receiveSocketArgs.RemoteEndPoint = localEndPoint;
            receiveSocketArgs.Completed += IO_Completed;
            receiveSocketArgs.SetBuffer(receivebuffer, 0, receivebuffer.Length);
            AsyncUdpUserToken token = new AsyncUdpUserToken();
            token.Socket = socket;
            token.IPAddress = localEndPoint.Address;

        }


        /// <summary>
        /// 开始接收数据
        /// </summary>
        public void StartReceive()
        {
            if (!socket.ReceiveFromAsync(receiveSocketArgs))
            {
                ProcessReceived(receiveSocketArgs);
            }

        }


        /// <summary>
        /// 处理接收的数据
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="Offset"></param>
        /// <param name="len"></param>
        /// <param name="isCache"></param>
        private void DoEventRecvice(byte[]buf, EndPoint remote, int Offset=0,int len=0,bool isCache=false)
        {
            AsyncUdpUserToken token=tokenPool.Pop();
            token.Data = buf;
            token.Offset = Offset;
            token.Length = len;
            token.IsFixCache = IsFixCache;
            token.Remote = remote;
            token.Socket = socket;
            if (isCache)
            {
                token.Cache = cacheManager;
            }
            if(OnDataReceived!=null)
            {
                Task.Factory.StartNew(() =>
                {
                    OnDataReceived(this, token);
                });
                
            }
           

        }

        /// <summary>
        /// 接收完成处理
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceived(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                    byte[] buf = null;
                    bool r = false;
                    int index = 0;
                    int len = e.BytesTransferred;
                if (IsFixCache)
                    {
                        if (cacheManager.SetBuffer(out buf, out index))
                        {
                            r = true;
                        }
                    }
                    else
                    {
                        if(cacheManager.GetBuffer(out buf))
                        {
                            r = true;
                        }
                    }
                    //
                    if(!r)
                    {
                        buf = new byte[len];
                    }
                    //
                    Array.Copy(e.Buffer, e.Offset, buf,index, len);
                  
                    if(EnableHeart)
                    {
                        //
                        AsyncUdpUserToken cur = null;
                        IPEndPoint remote = e.RemoteEndPoint as IPEndPoint;
                        string id = remote.ToString() + remote.Port;
                        if (dicToken.TryGetValue(id,out cur))
                        {
                            cur.DataTime = DateTime.Now;
                        }
                        else
                        {
                            cur = new AsyncUdpUserToken();
                            cur.IPAddress = localEndPoint.Address;
                            cur.Socket = socket;
                            cur.Remote = e.RemoteEndPoint;
                            dicToken[id] = cur;
                        }
                       
                        bool rCpm = true;
                        if (len== HeartBytes.Length)
                        {
                               for (int i = 0; i < len; i++)
                                if (buf[i+ index] != e.Buffer[i + e.Offset])
                                {
                                    rCpm = false;
                                    break;
                                }
                        }
                        if(rCpm)
                        {
                            //是心跳包
                            StartReceive();
                            return;
                        }
                    }
                    //不要进行耗时操作
                    DoEventRecvice(buf,e.RemoteEndPoint,index,len,r);
                    
                
            }
            StartReceive();
        }

        /// <summary>
        /// 发送完成处理
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSent(SocketAsyncEventArgs e)
        {

            if(e.UserToken==null)
            {
                //说明是直接发送或者不够缓存时新创建的
                e.SetBuffer(null, 0, 0);
                e.Completed -= IO_Completed;
            }
            else
            {
               //来自外部缓存或者本层缓存
                AsyncUdpUserToken token = e.UserToken as AsyncUdpUserToken;
                if(null!=token)
                {
                    //来自外部数据
                    if(token.UserInfo== "outcache")
                    {
                        token.FreeCache();
                        e.SetBuffer(null, 0, 0);
                        e.Completed -= IO_Completed;
                    }
                }
                
               
                //else
                //{
                //    //回收缓存;如果发送每次在获取，则要释放
                //    //如果发送时判断分配缓存了，这里就可以不回收
                //    if (IsFixCache)
                //    {
                //        bufferManager.FreeBuffer(e);
                //    }
                //   else
                //    {
                //        bufferManager.GetBuffer(e);
                //    }

                //}
            }
            e.UserToken = null;
            pool.Push(e);

        }
       
        /// <summary>
        /// 数据直接发送
        /// </summary>
        /// <param name="content"></param>
        /// <param name="remoteEndPoint"></param>
        public void Send(byte[] content, EndPoint remoteEndPoint,int offset=0,int len=0)
        {
            SocketAsyncEventArgs socketArgs = pool.Pop();
            socketArgs.RemoteEndPoint = remoteEndPoint;
            if (len == 0)
            {
                len = content.Length;
            }
            //设置发送的内容
            if(socketArgs.Buffer!=null)
            {
               
                //先释放通信层的缓存,释放原理的缓存；UDP发送完成后没有回收缓存区
                    if (IsFixCache)
                    {
                        bufferManager.FreeBuffer(socketArgs);
                    }
                    else
                    {
                        bufferManager.FreePoolBuffer(socketArgs);
                    }
                
                socketArgs.SetBuffer(null, 0, 0);
               
            }
            else
            {
                socketArgs.Completed += IO_Completed;
            }
            socketArgs.SetBuffer(content, offset, len);
            if (socketArgs.RemoteEndPoint != null)
            {
                if (!socket.SendToAsync(socketArgs))
                {
                    ProcessSent(socketArgs);
                }
            }
        }
       
        /// <summary>
       /// 
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    this.ProcessReceived(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    this.ProcessSent(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive");
            }
        }

        /// <summary>
      /// 发送数据
      /// </summary>
      /// <param name="token"></param>
      /// <param name="isCache"></param>
        public void SendPackage(AsyncUdpUserToken token,int isCache = 0)
        {
            /*
             * 说明，通信层的缓存不回收，其余的数据均回收
             * 0.socketArgs.UserToken=null,直接设置buffer=null（标记UserToken=null）
             * 1.本层缓存，不回收buffer;(不做任何处理，每次有多个）
             * 2.外部缓存，回收直接设置buffer=null,同时回收使用缓存（每次只会有一个，直接回收）
             */
            if (0 == isCache)
            {
                Send(token.Data, token.Remote, token.Offset, token.Length);
            }
            else  if(1==isCache)
            {
                //使用通信缓存分组发送;
                //只有这个分支会用到通信缓存
                int index = token.Offset;
                do
                {
                    SocketAsyncEventArgs socketArgs = pool.Pop();
                    socketArgs.RemoteEndPoint = token.Remote;
                    socketArgs.UserToken = token;
                    token.Socket = socket;
                    token.UserInfo = "udpcache";
                    if (socketArgs.Buffer==null)
                    {
                        //没有分配缓存时才分配
                        if (IsFixCache)
                        {
                            bufferManager.SetBuffer(socketArgs);
                        }
                        else
                        {
                            bufferManager.GetBuffer(socketArgs);
                        }
                        socketArgs.Completed += IO_Completed;
             
                    }
                    if (token.Length == 0)
                    {
                        token.Length = token.Data.Length;
                    }
                    //拷贝数据到本缓存,
                    if (socketArgs.Count + index >= token.Length)
                    {
                        Array.Copy(token.Data, token.Offset + index, socketArgs.Buffer, socketArgs.Offset, socketArgs.Count);
                        index += socketArgs.Count;
                    }
                    else
                    {
                        //不够缓存发送了
                        byte[] tmp = new byte[token.Length - index];
                        Array.Copy(token.Data, token.Offset + index, tmp, 0, tmp.Length);
                        index += tmp.Length;
                       
                        //先释放
                        if (socketArgs.Buffer!=null)
                        {
                            //说明来自缓存
                            if (IsFixCache)
                            {
                                bufferManager.FreeBuffer(socketArgs);
                            }
                            else
                            {
                                bufferManager.FreePoolBuffer(socketArgs);
                            }
                            
                        }
                        else
                        {
                            //说明来自创建
                            socketArgs.SetBuffer(null, 0, 0);
                        }
                        socketArgs.SetBuffer(tmp, 0, tmp.Length);
                        socketArgs.UserToken = null;
                    }
                    //
                  
                    if (socketArgs.RemoteEndPoint != null)
                    {
                        if (!socket.SendToAsync(socketArgs))
                        {
                            ProcessSent(socketArgs);
                        }
                    }
                } while (index < token.Length);
                token.FreeCache();//用完外部的了；
            }
            else if(2==isCache)
            {
                SocketAsyncEventArgs socketArgs = pool.Pop();
                socketArgs.RemoteEndPoint = token.Remote;
                token.UserInfo = "outcache";
                if(token.Length==0)
                {
                    token.Length = token.Data.Length;
                }
                if (socketArgs.Buffer != null)
                {
                    if (IsFixCache)
                        {
                            bufferManager.FreeBuffer(socketArgs);
                        }
                        else
                        {
                            bufferManager.FreePoolBuffer(socketArgs);
                        }
                    
                    socketArgs.SetBuffer(null, 0, 0);
                }
                else
                {
                    socketArgs.Completed += IO_Completed;
                }
                //持续使用外部缓存发送，发送后要释放
                socketArgs.UserToken = token;
                socketArgs.SetBuffer(token.Data, token.Offset, token.Length);
                if (socketArgs.RemoteEndPoint != null)
                {
                    if (!socket.SendToAsync(socketArgs))
                    {
                        ProcessSent(socketArgs);
                    }
                }
            }
            else
            {
                Console.WriteLine("isCache参数不正确");
            }
        }

        /// <summary>
        /// 发送心跳
        /// </summary>
        /// <param name="endPoint"></param>
        public void SendHeart(EndPoint endPoint)
        {
            Send(HeartBytes, endPoint);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            socket.Close();
            pool.Clear();
            pool.Dispose();
            pool = null;
            bufferManager.Clear();
            tokenPool.Clear();
            dicToken.Clear();
        }

    }
}
