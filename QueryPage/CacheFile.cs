#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：CacheFile
* 类 描 述 ：
* 命名空间 ：QueryPage
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
using System.Text;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：CacheFile 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

   internal class CacheFile
    {
        public string DB;
        public string Name;
        public object Data;
        public bool isModel;
    }
}
