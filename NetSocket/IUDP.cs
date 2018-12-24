/**
* 命名空间: NetSocket 
* 类 名：IUDP 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：IUDP 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
  public  interface IUDP
    {
         void SendPackage(AsyncUdpUserToken token, int isCache = 0);
      
    }
}
