#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：QueryPageSQL
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
using System.Xml.Serialization;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：QueryPageSQLXML  读取配置
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    [XmlRoot("QueryPage")]
    internal class QueryPageSQLXML
    {
        [XmlElement("DB")]
        public List<DBSQL> DBSQLs { get; set; }
    }


    internal class DBSQL
    {
        public string DBName { get; set; }
        public List<PageSQL> PageSQLs { get; set; }
    }

    internal class PageSQL
    {
        public string QueryName { get; set; }
        public string SQL { get; set; }

        public long ID { get; set; }
    }
}
