/**
* 命名空间: NetSocket 
* 类 名：UDPSocket 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;

namespace NetSocket
{

    public delegate void OnReceiveTaskUdpData(object sender, AsyncUdpUserToken token);
    /// <summary>
    /// 功能描述    ：UDPSocket UDP通信，所有属性Bind之前设置
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class UDPTaskSocket: IUDP
    {
        private Socket socket;

        private Semaphore semaphore = null;

        private SocketAsyncEventArgs receiveSocketArgs;

        private AsyncSocketUDPState receiveState = null;

        private IPEndPoint localEndPoint;

        private byte[] receivebuffer;

        private CacheManager cacheManager = null;

        private UserTokenPool tokenPool = null;

        private ConcurrentQueue<AsyncUDPSendBuffer> queue;

        private UDPSendPool uDPSendPool = null;

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

        public UDPTaskSocket()
        {
            BufferSize = 65535;
            TotalBufSize = 1024*MByte;
            IsFixCache = false;
            cacheManager = new CacheManager();
            tokenPool = new UserTokenPool();
            uDPSendPool = new UDPSendPool();
            queue = new ConcurrentQueue<AsyncUDPSendBuffer>();
            TokenMaxLeftNum = Environment.ProcessorCount*10;
            if(TokenMaxLeftNum == 0)
            {
                TokenMaxLeftNum = 100;
            }
            int CPU =(int)(Environment.ProcessorCount*1.5);
            semaphore = new Semaphore(CPU,CPU);
        }


        public void Bind()
        {
            
          
            cacheManager.BufferSize = BufferSize;
        
            cacheManager.Capacity = TotalBufSize*MByte;
            tokenPool.MaxLeftNum = TokenMaxLeftNum;
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
             socket.SendBufferSize = 64 * 1024;
            socket.ReceiveBufferSize = 64 * 1024;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000);
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
            receiveState = new AsyncSocketUDPState();
            receiveState.Data = receivebuffer;
            receiveState.Remote = localEndPoint;
            receiveState.IPAddress = localEndPoint.Address;
            receiveState.Socket = socket;
        

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
        public void Recvice()
        {
            socket.BeginReceiveFrom(receivebuffer, 0, receivebuffer.Length,SocketFlags.None,ref  receiveState.Remote, Recieve, receiveState);
        }

         private void Recieve(IAsyncResult ar)
        {

            AsyncSocketUDPState so = ar.AsyncState as AsyncSocketUDPState;
            int len = -1;
            try
            {
                len = socket.EndReceiveFrom(ar, ref so.Remote);
                //
                byte[] buf = null;
                bool r = false;
                int index = 0;
                if (IsFixCache)
                {
                    if (cacheManager.SetBuffer(out buf, out index))
                    {
                        r = true;
                    }
                }
                else
                {
                    if (cacheManager.GetBuffer(out buf))
                    {
                        r = true;
                    }
                }
                //
                if (!r)
                {
                    buf = new byte[len];
                }
                //
                Array.Copy(receiveState.Data, 0, buf, index, len);

                if (EnableHeart)
                {
                    //
                    AsyncUdpUserToken cur = null;
                    IPEndPoint remote = so.Remote as IPEndPoint;
                    string id = remote.ToString() + remote.Port;
                    if (dicToken.TryGetValue(id, out cur))
                    {
                        cur.DataTime = DateTime.Now;
                    }
                    else
                    {
                        cur = new AsyncUdpUserToken();
                        cur.IPAddress = localEndPoint.Address;
                        cur.Socket = socket;
                        cur.Remote = so.Remote;
                        dicToken[id] = cur;
                    }

                    bool rCpm = true;
                    if (len == HeartBytes.Length)
                    {
                        for (int i = 0; i < len; i++)
                            if (buf[i + index] != so.Data[i +so.OffSet ])
                            {
                                rCpm = false;
                                break;
                            }
                    }
                    if (rCpm)
                    {
                        //是心跳包
                        Recvice();
                        return;
                    }
                }
                //不要进行耗时操作
                DoEventRecvice(buf, so.Remote, index, len, r);
            }
            catch (Exception)
            {
                //TODO 处理异常

            }
            finally
            {
                Recvice();
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
                    if (cacheManager.GetBuffer(out buf))
                    {
                        r = true;
                    }
                }
                //
                if (!r)
                {
                    buf = new byte[len];
                }
                //
                Array.Copy(e.Buffer, e.Offset, buf, index, len);

                if (EnableHeart)
                {
                    //
                    AsyncUdpUserToken cur = null;
                    IPEndPoint remote = e.RemoteEndPoint as IPEndPoint;
                    string id = remote.ToString() + remote.Port;
                    if (dicToken.TryGetValue(id, out cur))
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
                    if (len == HeartBytes.Length)
                    {
                        for (int i = 0; i < len; i++)
                            if (buf[i + index] != e.Buffer[i + e.Offset])
                            {
                                rCpm = false;
                                break;
                            }
                    }
                    if (rCpm)
                    {
                        //是心跳包
                        StartReceive();
                        return;
                    }
                }
                //不要进行耗时操作
                DoEventRecvice(buf, e.RemoteEndPoint, index, len, r);
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
          

        }
       
        /// <summary>
        /// 数据直接发送
        /// </summary>
        /// <param name="content"></param>
        /// <param name="remoteEndPoint"></param>
        public void Send(byte[] content, EndPoint remoteEndPoint,int offset=0,int len=0)
        {
         
            if (len == 0)
            {
                len = content.Length;
            }
            AsyncUDPSendBuffer sendBuffer = uDPSendPool.Pop();
            //socket.SendTo(content, offset, len, SocketFlags.None, remoteEndPoint);
            sendBuffer.Buffer = content;
            sendBuffer.Offset = offset;
            sendBuffer.Length = len;
            sendBuffer.EndPoint = remoteEndPoint;
            queue.Enqueue(sendBuffer);
            
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
                byte[] sendBuffer = null;
                int Offset = 0;
                int len = 0;
                do
                {
                 
                   token.Socket = socket;
                   token.UserInfo = "udpcache";
                    
                    if (token.Length == 0)
                    {
                        token.Length = token.Data.Length;
                    }
                    //拷贝数据到本缓存
                    AsyncUDPSendBuffer buffer = uDPSendPool.Pop();
                    buffer.EndPoint = token.Remote;
                    if (IsFixCache)
                    {
                        cacheManager.SetBuffer(out sendBuffer, out Offset);
                    }
                    else
                    {
                        cacheManager.GetBuffer(out sendBuffer);
                    }
                    if (cacheManager.BufferSize + index >= token.Length)
                    {
                        Array.Copy(token.Data, token.Offset + index, sendBuffer, Offset, cacheManager.BufferSize);
                        index += cacheManager.BufferSize;
                        len = cacheManager.BufferSize;
                        //
                        buffer.Offset = Offset;
                        buffer.Length = len;
                        buffer.BufferCache = cacheManager;
                        buffer.IsFixCache = IsFixCache;

                    }
                    else
                    {
                        //不够缓存发送了
                        byte[] tmp = new byte[token.Length - index];
                        Array.Copy(token.Data, token.Offset + index, tmp, 0, tmp.Length);
                        index += tmp.Length;
                        len = tmp.Length;
                        buffer.Length = len;
                        buffer.Offset = 0;
                        buffer.Buffer = tmp;
                    }
                    //
                    //socket.SendTo(sendBuffer, Offset, len,SocketFlags.None,token.Remote);
                 
                    queue.Enqueue(buffer);
                    
                } while (index < token.Length);
                token.FreeCache();//用完外部的了；
            }
            else if(2==isCache)
            {
               
                token.UserInfo = "outcache";
                if(token.Length==0)
                {
                    token.Length = token.Data.Length;
                }

                AsyncUDPSendBuffer buffer = uDPSendPool.Pop();
                buffer.Buffer = token.Data;
                buffer.Offset = token.Offset;
                buffer.Length = token.Length;
                buffer.EndPoint = token.Remote;
                buffer.Token = token;//外部缓存发送完成释放
                queue.Enqueue(buffer);
                //持续使用外部缓存发送，发送后要释放
            


            }
            else
            {
                Console.WriteLine("isCache参数不正确");
            }
            TaskSend();
        }


        /// <summary>
        /// 开启线程发送
        /// 每次发送时调用
        /// </summary>
        private void TaskSend()
        {
            if(!semaphore.WaitOne(100))
            {
                return;
            }
            if (!queue.IsEmpty)
            {
                Task.Factory.StartNew(() =>
                {
                    AsyncUDPSendBuffer buffer = null;
                    do
                    {
                        if (queue.TryDequeue(out buffer))
                        {
                            socket.SendTo(buffer.Buffer, buffer.Offset, buffer.Length, SocketFlags.None, buffer.EndPoint);
                           
                            buffer.FreeDataCache();
                            buffer.Free();
                            if (buffer.Token != null)
                            {
                                AsyncUdpUserToken token = buffer.Token as AsyncUdpUserToken;
                                if (null != token)
                                {
                                    //外部缓存也释放
                                    token.FreeCache();
                                }
                            }
                        }
                    }
                    while (!queue.IsEmpty);
                     semaphore.Release();
                });
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
           
            tokenPool.Clear();
            dicToken.Clear();
        }

    }
}
    
