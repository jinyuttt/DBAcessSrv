#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：ClientData
* 类 描 述 ：
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/16 19:09:46
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：ClientData 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
  public  struct ClientData
    {
        public string ClientFlage;
        public byte[] Data;
    }
}
