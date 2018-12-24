#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：DBPagePool
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
using System.Collections.Concurrent;
using System.Data;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：DBPagePool 保持每个SQL语句的缓存
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    internal class DBPagePool
    {
        /// <summary>
        /// 使用的数据库名称（配置名称）
        /// </summary>
        public string DBName { get; set; }

        public int PageCacheNum { get; set; }

        /// <summary>
        /// 查询ID，数据
        /// 每个分页SQL的数据
        /// </summary>
        private ConcurrentDictionary<long, DBQueryCache> dicQuery = null;


        public DBPagePool(int pageCacheNum)
        {
            PageCacheNum = pageCacheNum;
        }

        /// <summary>
        /// 验证缓存有效性
        /// </summary>
        /// <param name="timeLen"></param>
        public void Validate(int timeLen)
        {
            foreach(DBQueryCache cache in dicQuery.Values)
            {
                cache.Validate(timeLen);
            }
        }

        public void Clear()
        {
            foreach (DBQueryCache cache in dicQuery.Values)
            {
                cache.Clear();
            }
        }


            /// <summary>
            /// 添加分页缓存
            /// </summary>
            /// <param name="queryID">查询ID</param>
            /// <param name="pageNum">分页页号</param>
            /// <param name="models">数据</param>
            public List<object> Add(long queryID, int pageNum, List<object> models)
        {
            DBQueryCache queryCache = null;
            if (dicQuery.TryGetValue(queryID, out queryCache))
            {
                return queryCache.Add(pageNum, models);
            }
            else
            {
                queryCache = new DBQueryCache(PageCacheNum) { QueryID = queryID };
                dicQuery[queryID] = queryCache;
                return queryCache.Add(pageNum, models);
            }
        }

        /// <summary>
        ///  添加分页缓存
        /// </summary>
        /// <param name="queryID"></param>
        /// <param name="pageNum"></param>
        /// <param name="dt"></param>
        public DataTable Add(long queryID, int pageNum, DataTable dt)
        {
            DBQueryCache queryCache = null;
            if (dicQuery.TryGetValue(queryID, out queryCache))
            {
                return queryCache.Add(pageNum, dt);
            }
            else
            {
                queryCache = new DBQueryCache(PageCacheNum) { QueryID = queryID };
                dicQuery[queryID] = queryCache;
                return queryCache.Add(pageNum, dt);
            }
        }

        public List<object> GetListModel(long queryID, int pageNum)
        {
            DBQueryCache queryCache = null;
            if (dicQuery.TryGetValue(queryID, out queryCache))
            {
                return queryCache.GetListModel(pageNum);
            }
            return null;
        }

        public DataTable GetDataTable(long queryID, int pageNum)
        {
            DBQueryCache queryCache = null;
            if (dicQuery.TryGetValue(queryID, out queryCache))
            {
                return queryCache.GetDataTable(pageNum);
            }
            return null;
        }
    }
}
