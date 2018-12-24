/**
* 命名空间: NetSocket 
* 类 名：UDPPack 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：UDPPack 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
  public  class UDPPack
    {
        UDPTaskSocket uDPSocket = null;
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

        /// <summary>
        /// 缓存数据实体
        /// 默认100
        /// </summary>
        public int TokenMaxLeftNum { get; set; }

        /// <summary>
        /// 接收是否解包
        /// 只抛出数据
        /// </summary>
        public  bool IsProtolUnPack { get; set; }

        public event OnReceiveUdpData OnDataReceived;
        private const int MByte = 1024 * 1024;

        public UDPPack()
        {
            BufferSize = 65535;
            TotalBufSize =2* 1024;
            IsFixCache = false;

            TokenMaxLeftNum = Environment.ProcessorCount * 10;
            if (TokenMaxLeftNum == 0)
            {
                TokenMaxLeftNum = 100;
            }

        }


        /// <summary>
        /// 绑定
        /// </summary>
        public void Bind()
        {
            if(uDPSocket!=null)
            {
                uDPSocket.Close();
            }
            uDPSocket = new UDPTaskSocket();
            uDPSocket.BufferSize = BufferSize;
            uDPSocket.EnableHeart = EnableHeart;
            uDPSocket.Host = Host;
            uDPSocket.IsFixCache = IsFixCache;
            uDPSocket.Port = Port;
            uDPSocket.TotalBufSize = TotalBufSize;
            uDPSocket.TokenMaxLeftNum = TokenMaxLeftNum;
            uDPSocket.Bind();
            uDPSocket.OnDataReceived += UDPSocket_OnDataReceived;

        }

        private void UDPSocket_OnDataReceived(object sender, AsyncUdpUserToken token)
        {
            if(IsProtolUnPack)
            {
                token.Offset = token.Offset + UDPDataPackage.HeadLen;
            }
            if(OnDataReceived!=null)
            {
                OnDataReceived(this, token);
            }
        }

        /// <summary>
        /// 接收
        /// </summary>
        public  void StartReceive()
        {
            uDPSocket.StartReceive();
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        private void Send(byte[] data,string host,int port, int offset = 0,int len=0)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(host), port);
            uDPSocket.Send(data, endPoint, offset, len);
        }

        /// <summary>
        /// 发送数据
        /// isCache：
        /// 0直接使用数据区发送（没有缓存）
        /// 1拷贝通信层缓存发送（分割缓存大小发送）
        /// 2外部缓存发送，发送完成释放
        /// </summary>
        /// <param name="token">数据封</param>
        /// <param name="isCache">
        /// 0直接使用数据区发送（没有缓存）
        /// 1拷贝发送层缓存发送（分割缓存大小发送）
        /// 2外部缓存发送，发送完成释放
        /// 
        /// </param>
        public void Send(AsyncUdpUserToken token,int isCache)
        {
            uDPSocket.SendPackage(token, isCache);
        }

        /// <summary>
        /// 数据分包；采用分包层缓存分包
        /// 分包就不可能使用外部缓存
        /// 
        /// </summary>
        /// <param name="token"></param>
        public void SendPackage(AsyncUdpUserToken token)
        {
            //使用了分包缓存
            int index = 0;
            if(token.Length==0)
            {
                token.Length = token.Data.Length;
            }
            do
            {
                AsyncUdpUserToken userToken= DataPack.PackCacheUDP(token, ref index);
                Send(userToken, 2);
            } while (index < token.Length);
            token.FreeCache();
        }

       
        /// <summary>
        /// 数据按照分包层大小分包发送
        /// 没有协议
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="host">远端IP</param>
        /// <param name="port">远端端口</param>
        /// <param name="isCache">是否使用分包层缓存分包</param>
        /// <param name="offet">数据区偏移</param>
        /// <param name="len">数据区长度</param>
        public void SendPackage(byte[] data,string host,int port, bool isCache = false,int offet=0,int len=0)
        {
            //使用了分包缓存
            int index = 0;
            AsyncUdpUserToken token = null;
            if (len == 0)
            {
                len = data.Length;
            }
            if (token == null)
            {
                token = new AsyncUdpUserToken();
                token.Data = data;
                token.Offset = offet;
                token.Length = len;
                token.Remote = new IPEndPoint(IPAddress.Parse(host), port);
                
            }
            do
            {
                if(isCache)
                {
                   
                   SendPackage(DataPack.PackCacheUDP(token, ref index));
                   
                }
                else
                {
                    Send(DataPack.PackUDP(token, ref index),host,port);
                }
            } while (index <len);
            token.FreeCache();
        }

        /// <summary>
        /// 按照协议分包组包发送
        /// </summary>
        /// <param name="token"></param>
        public void SendProtol(AsyncUdpUserToken token)
        {
            //使用了分包缓存
            int index = 0;
            if (token.Length == 0)
            {
                token.Length = token.Data.Length;
            }
            do
            {
                AsyncUdpUserToken userToken = DataPack.PackCacheUDPHead(token, ref index);
                userToken.Remote = token.Remote;
                Send(userToken, 2);
                if(token.ListPack!=null)
                {
                    token.ListPack.Add(userToken);
                }
            } while (index < token.Length);
            token.FreeCache();
        }

       /// <summary>
       /// 按照协议分包组包发送
       /// 一定用到分包层缓存
       /// </summary>
       /// <param name="data"></param>
       /// <param name="host"></param>
       /// <param name="port"></param>
       /// <param name="offet"></param>
       /// <param name="len"></param>
        public void SendProtol(byte[] data, string host, int port, int offet = 0, int len = 0)
        {
                AsyncUdpUserToken token = new AsyncUdpUserToken();
                token.Data = data;
                token.Offset = offet;
                token.Length = len;
                token.Remote = new IPEndPoint(IPAddress.Parse(host), port);
            
                SendProtol(token);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            uDPSocket.Close();
        }
    }
}
