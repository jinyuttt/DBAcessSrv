/**
* 命名空间: NetSocket 
* 类 名：AsyncUserToken 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Net;
using System.Net.Sockets;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：AsyncUserToken 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class AsyncTcpUserToken: AsyncUserToken
    {
        /// <summary>
        /// 服务端绑定对象
        /// </summary>
        private IServer server = null;

        /// <summary>
        /// 客户端绑定对象
        /// </summary>
        private TCPClient client = null;

       

        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime ConnectTime { get; set; }

      


        /// <summary>
        /// 服务端绑定
        /// </summary>
        public IServer Server { set { server = value; } }

        /// <summary>
        /// 绑定的客户端
        /// </summary>
        public TCPClient Client { set { client = value; } }

        public AsyncTcpUserToken()
        {
            
        }


       /// <summary>
       /// 直接发送数据;
       /// 交给服务端处理；
       /// </summary>
       /// <param name="data"></param>
        public void SendData(byte[] data)
        {
            if(server!=null)
            {
                server.SendPackage(this, data);
            }
        }
    
       /// <summary>
       ///打包发送
       ///如果是底层类则不会分包
       /// </summary>
       /// <param name="data">发送数据</param>
       /// <param name="offset">数据偏移</param>
       /// <param name="len">数据长度</param>
       /// <param name="isClientCache">是否使用缓存打包，-1 不打包直接发送，0打包新数组直接发送，1直接使用客户端缓存发送，2使用服务端缓存发送</param>
       public void SendPack(byte[]data,int offset=0,int len=0,int isClientCache=-1)
        {
            server.SendPackage(this, data, offset, len, isClientCache);
        }

        
        /// <summary>
        /// 关闭客户端
        /// </summary>
        public void Close()
        {
            if (server != null)
            {
                server.CloseClient(this);
            }
            if(client!=null)
            {
                client.Dispose();
            }
        }
    }
}
