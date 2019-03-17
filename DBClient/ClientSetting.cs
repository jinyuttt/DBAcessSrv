#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DBClient
* 项目描述 ：
* 类 名 称 ：ClientSetting
* 类 描 述 ：
* 命名空间 ：DBClient
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 23:59:32
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Collections.Generic;
using System.Text;

namespace DBClient
{
    /* ============================================================================== 
* 功能描述：ClientSetting 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
   public class ClientSetting
    {
       
        /// <summary>
        /// 是否有文件授权验证
        /// </summary>
        public static bool IsFileauthorization { get; set; }

        /// <summary>
        /// 验证文件路径
        /// </summary>
        public static string AuthorizationFile { get; set; }
    }
}
