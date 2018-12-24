/**
* 命名空间: NetSocket 
* 类 名：IServer 
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
    /// 功能描述    ：IServer 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
   public interface IServer
    {
         void SendPackage(AsyncTcpUserToken token, byte[] message,int offset=0,int len=0,int isCache=0);
        void CloseClient(AsyncTcpUserToken asyncUserToken);
    }
}
