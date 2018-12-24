#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：QueryCache
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：QueryCache 分页处理缓存
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    internal class QueryCache
    {
        /// <summary>
        /// 每个db分页SQL的数据
        /// </summary>
        private ConcurrentDictionary<string, DBPagePool> dicQuery = null;


        /// <summary>
        /// 缓存实体个数
        /// </summary>
        public int PageCacheNum { get; set; }


        /// <summary>
        /// 验证有效性
        /// </summary>
        /// <param name="timeLen"></param>
        public void Validate(int timeLen)
        {
            foreach (DBPagePool cache in dicQuery.Values)
            {
                cache.Validate(timeLen);
            }
        }

        public List<object> Add(string db, long queryID, int pageNum, List<object> models)
        {
            DBPagePool entity = null;
            if (dicQuery.TryGetValue(db, out entity))
            {
               return entity.Add(queryID, pageNum, models);
            }
            else
            {
                entity = new DBPagePool(PageCacheNum);
                dicQuery[db] = entity;
              return  entity.Add(queryID, pageNum, models);
            }
        }
        public DataTable Add(string db, long queryID, int pageNum, DataTable dt)
        {
            DBPagePool entity = null;
            if (dicQuery.TryGetValue(db, out entity))
            {
              return  entity.Add(queryID, pageNum, dt);
            }
            else
            {
                entity = new DBPagePool(PageCacheNum);
                dicQuery[db] = entity;
               return  entity.Add(queryID, pageNum, dt);
            }
        }
        public List<object> GetListModel(string db, long queryID, int pageNum)
        {
            DBPagePool entity = null;
            if (dicQuery.TryGetValue(db, out entity))
            {
              return  entity.GetListModel(queryID, pageNum);
            }
            return null;
        }

        public DataTable GetDataTable(string db, long queryID, int pageNum)
        {
            DBPagePool entity = null;
            if (dicQuery.TryGetValue(db, out entity))
            {
               return entity.GetDataTable(queryID, pageNum);
            }
            return null;
        }
    }
}
