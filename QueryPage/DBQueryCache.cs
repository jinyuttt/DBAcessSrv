#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：PageManager
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
using System.Threading;
using System.Threading.Tasks;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：DBQueryCache 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    internal class DBQueryCache
    {
        public  long QueryID { get; set; }

        private  int PageCacheNum { get; set; }

        private CacheEntity[] m_models = null;
        private CacheEntity[] m_tables = null;
        private int modelIndex = 0;
        private int dtIndex = 0;
        private Dictionary<int, int> dicModelIndex = null;
        private Dictionary<int, int> dicDtIndex = null;

        private ReaderWriterLockSlim  lockDt;
        private ReaderWriterLockSlim lockmodel;
        public DBQueryCache(int num)
        {
            PageCacheNum = num;
            dicModelIndex = new Dictionary<int, int>();
            dicDtIndex = new Dictionary<int, int>();
            m_models = new CacheEntity[num];
            m_tables = new CacheEntity[num];
            lockDt = new ReaderWriterLockSlim();
            lockmodel = new ReaderWriterLockSlim();

        }


        /// <summary>
        /// 分页数据缓存
        /// </summary>
        /// <param name="pageNum">分页页号</param>
        /// <param name="model">Model数据</param>
        public List<object> Add(int pageNum, List<object> model)
        {
            lockmodel.EnterWriteLock();
            List<object> list = null;
            try
            {
                int curIndex = Interlocked.Increment(ref modelIndex) % PageCacheNum;
                if (m_models[curIndex] != null)
                {
                    dicModelIndex.Remove(m_models[curIndex].PageNum);
                    list = m_models[curIndex].Model;
                }
                m_models[curIndex] = new CacheEntity(model,pageNum);
                dicModelIndex[pageNum] = curIndex;
            }
            finally
            {
                lockmodel.ExitWriteLock();
            }
           
            return list;
        }

        /// <summary>
        /// 添加分页缓存
        /// </summary>
        /// <param name="pageNum">页号</param>
        /// <param name="data">数据</param>
        public DataTable Add(int pageNum,DataTable data)
        {
            lockDt.EnterWriteLock();
            DataTable table = null;
            try
            {
                int curIndex = Interlocked.Increment(ref dtIndex) % PageCacheNum;
                if (m_tables[curIndex] != null)
                {
                    dicDtIndex.Remove(m_tables[curIndex].PageNum);
                    table = m_tables[curIndex].DataTable;
                }
                m_tables[curIndex] = new CacheEntity(data,pageNum);
                dicDtIndex[pageNum] = curIndex;
              
            }
            finally
            {
                lockDt.ExitWriteLock();
            }
            return table;
        }

        /// <summary>
        /// 获取model
        /// </summary>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public List<object> GetListModel(int pageNum)
        {
            int index = -1;
            lockmodel.EnterUpgradeableReadLock();
            try
            {
                if (dicModelIndex.TryGetValue(pageNum, out index))
                {
                    return m_models[index].Model;
                }
            }
            finally
            {
                lockmodel.ExitUpgradeableReadLock();
            }
            return null;
        }

        /// <summary>
        /// 获取data
        /// </summary>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public DataTable GetDataTable(int pageNum)
        {
            int index = -1;
            lockDt.EnterUpgradeableReadLock();
            try
            {
                if (dicDtIndex.TryGetValue(pageNum, out index))
                {
                    return m_tables[index].DataTable;
                }
            }
            finally
            {
                lockDt.ExitUpgradeableReadLock();
            }
            return null;
        }

        /// <summary>
        /// 监测移除的数据。
        /// </summary>
        /// <param name="timeLen"></param>
        public void Validate(int timeLen)
        {
            Task.Factory.StartNew(() =>
            {
                 ProcessValidate(m_tables, timeLen, dicDtIndex, lockDt);
                 ProcessValidate(m_models, timeLen,dicModelIndex,lockmodel);
                
            });

        }

       
        /// <summary>
        /// 监测
        /// </summary>
        /// <param name="caches"></param>
        /// <param name="timeLen"></param>
        /// <returns></returns>
        private void ProcessValidate(CacheEntity[] caches,int timeLen, Dictionary<int, int> index, ReaderWriterLockSlim lockSlim)
        {
            //如果是大数据量，要考虑设置Parallel线程
            DateTime cur = DateTime.Now;
         
            lockSlim.EnterWriteLock();
            try
            {
                Parallel.For(0, PageCacheNum, (i, loopState) =>
                {
                    CacheEntity entity = caches[i];
                    if (entity == null)
                    {
                        loopState.Break();
                        return;
                    }
                    else
                    {
                        if ((cur - entity.DateTime).TotalMinutes > timeLen)
                        {

                            index.Remove(entity.PageNum);
                            caches[i] = null;
                        }
                    }
                });
            }
            finally
            {
                lockSlim.ExitWriteLock();
            }
           
        }

        public void Clear()
        {
            dicDtIndex.Clear();
            dicModelIndex.Clear();
            m_tables = new CacheEntity[PageCacheNum];
            m_models = new CacheEntity[PageCacheNum];
        }
    }
}
