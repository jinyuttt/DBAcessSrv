/**
* 命名空间: NetSocket.Common 
* 类 名：UDPUserTocken 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：UDPUserTocken 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
  public  class AsyncUserToken
    {
        /// <summary>  
        /// 客户端IP地址  
        /// </summary>  
        public IPAddress IPAddress { get; set; }

        /// <summary>  
        /// 远程地址  
        /// </summary>  
        public EndPoint Remote { get; set; }

        /// <summary>  
        /// 通信SOKET  
        /// </summary>  
        public Socket Socket { get; set; }

        /// <summary>  
        /// 连接时间  
        /// </summary>  
        public DateTime CreateTime { get; set; }

        /// <summary>  
        /// 所属用户信息  
        /// </summary>  
        public string UserInfo { get; set; }

        /// <summary>
        /// 数据,客户端对外
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 标识
        /// </summary>
        public long TokenID { get; set; }

       /// <summary>
       /// 数据时间
       /// </summary>
        public DateTime DataTime { get; set; }
        
        /// <summary>
        /// 是否是固定大小缓存
        /// </summary>
        public bool IsFixCache { get; set; }
    }
}
