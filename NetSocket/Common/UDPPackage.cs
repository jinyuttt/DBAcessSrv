/**
* 命名空间: NetSocket 
* 类 名：UDPPackage 
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
    /// 功能描述    ：UDPPackage 0 数据 1 小包接收 2 丢失 3完成整包
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
  public  class UDPPackage
    {
        /// <summary>
        /// 类型
        /// </summary>
        public byte packageType = 0;

        /// <summary>
        /// 通信标识
        /// </summary>
        public long socketID = 0;

        /// <summary>
        /// 包ID
        /// </summary>
        public long packageID = 0;

        /// <summary>
        /// 包序列
        /// </summary>
        public int  packageSeq = 0;
    }
}
