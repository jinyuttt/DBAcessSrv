#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetSocket
* 项目描述 ：
* 类 名 称 ：AsyncSocketUDPState
* 类 描 述 ：
* 命名空间 ：NetSocket
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2018
* 更新时间 ：2018
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2018. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetSocket
{
    /* ============================================================================== 
    * 功能描述：AsyncSocketUDPState 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

  public  class AsyncSocketUDPState
    {
        /// <summary>  
        /// 客户端IP地址  
        /// </summary>  
        public IPAddress IPAddress { get; set; }

        /// <summary>  
        /// 远程地址  
        /// </summary>  
        public EndPoint Remote;

        /// <summary>  
        /// 通信SOKET  
        /// </summary>  
        public Socket Socket { get; set; }

        /// <summary>
        /// 数据,客户端对外
        /// </summary>
        public byte[] Data { get; set; }

        public int Length { get; set; }

        public int OffSet { get; set; }
    
      
      
    }
}
