#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DBModel
* 项目描述 ：
* 类 名 称 ：DataFieldAttribute
* 类 描 述 ：
* 命名空间 ：DBModelT 
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

namespace DBModel
{
    /* ============================================================================== 
    * 功能描述：DataFieldAttribute 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    public class DataFieldAttribute: Attribute
    {
      
         public string ColumnName { set; get; }

        public DataFieldAttribute(string columnName)
        {
           ColumnName = columnName;
        }
}
}
