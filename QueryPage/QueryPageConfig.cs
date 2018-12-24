#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：QueryPageConfig
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
using System.IO;
using System.Text;
using System.Reflection;
namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：QueryPageConfig 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    /// <summary>
    /// 缓存配置
    /// </summary>
   public class QueryPageConfig
    {
        public QueryPageConfig()
        {
            PageCacheNum = 50;
            CacheTime = 10;
            PageTemplate = "pagecfg";
            FileCacheDir = "CacheFile";
            PageSize = 100;
        }
        /// <summary>
        /// 是否缓存到文件
        /// 默认:false
        /// </summary>
        public bool IsCacheFile { get; set; }
        
        /// <summary>
        /// 每个SQL分页缓存的页码个数
        /// 默认50；注意缓存大小在内存
        /// </summary>
        public int PageCacheNum { get; set; }

       /// <summary>
       /// 分页大小
       /// 默认：100
       /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// 是否缓存在内存
        /// 默认：false
        /// </summary>
        public bool isMemory { get; set; }

        /// <summary>
        /// 分页模板配置目录;获取里面的XML
        /// 默认：pagecfg
        /// </summary>
        public string PageTemplate { get; set; }

        /// <summary>
        /// 文件缓存时的目录
        /// 默认：CACHEFILE
        /// </summary>
        public string FileCacheDir { get; set; }

        /// <summary>
        /// 每个缓存数据实体的有效时间（分钟）
        /// 默认：10分钟
        /// 如果分页数据实时性高，你要提高实时性，则必须缩短
        /// 当然还有就是直接读写分离
        /// </summary>
        public int CacheTime { get; set; }

        /// <summary>
        /// 加载配置文件
        /// 默认pagecfg\config.txt
        /// </summary>
        /// <param name="path"></param>
        public void LoadConfig(string path=null)
        {
            if(string.IsNullOrEmpty(path))
            {
                path = Path.Combine("pagecfg", "config.txt");
            }
            if(!File.Exists(path))
            {
                return;
            }
            //
            Dictionary<string, string> dic = new Dictionary<string, string>();
            using (StreamReader rd = new StreamReader(path))
            {
                string[] cfg = null;
                string line = null;
                while(rd.Peek()!=-1)
                {
                    line= rd.ReadLine();
                    if(!string.IsNullOrEmpty(line))
                    {
                        cfg = line.Split('=');

                        if (cfg.Length == 2)
                        {
                            if (string.IsNullOrEmpty(cfg[0]) || string.IsNullOrEmpty(cfg[1]))
                            {
                                continue;
                            }
                          dic[cfg[0].Trim().ToLower()] = cfg[1].Trim();
                        }
                    }
                   
                }
            }
            //
            string value = null;
            PropertyInfo[] properties = typeof(QueryPageConfig).GetProperties();
            foreach(PropertyInfo property in properties)
            {
                if(dic.TryGetValue(property.Name.ToLower(),out value))
                {
                    property.SetValue(this, Convert.ChangeType(value, property.PropertyType));
                }
            }

        }
    }
}
