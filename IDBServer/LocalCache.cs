#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DBServer
* 项目描述 ：
* 类 名 称 ：LocalCache
* 类 描 述 ：
* 命名空间 ：DBServer
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
using System.Threading.Tasks;

namespace DBServer
{
    /* ============================================================================== 
    * 功能描述：LocalCache 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

  public  class LocalCache<TKey,TValue>
    {
        LRUCache<TKey, TValue> cache = null;
        public LocalCache(int capacity=int.MaxValue/2)
        {
            cache = new LRUCache<TKey, TValue>(capacity);
        }
        public void Insert(TKey key,TValue value)
        {
            cache.Set(key, value);
        }
        public void Insert(Dictionary<TKey,TValue> data)
        {
            foreach(KeyValuePair<TKey,TValue> kv in data)
            {
                cache.Set(kv.Key, kv.Value);
            }
        }

        public bool GetValue(TKey key,out TValue value)
        {
            return cache.TryGet(key, out value);
        }

        public Dictionary<TKey,TValue> GetValues(TKey[] keys)
        {
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>(keys.Length);
            Parallel.ForEach(keys, key => {
                TValue value;
                if(cache.TryGet(key,out value))
                {
                    result[key] = value;
                }
            });
            return result;
        }


        public void Remove(TKey[] keys)
        {
            Parallel.ForEach(keys, key =>
            {
                Remove(key);
            });
        }
        public void Remove(TKey key)
        {
            cache.TryRemove(key);
        }

        public void Clear()
        {
            cache.Clear();
        }
    }
}
