using BerkeleyDB;
using Serializer;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
namespace BDB
{

    /// <summary>
    /// 对外提供的操作类
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class BerkeleyDBHelper<TKey,TValue>: BDBOpt
    {
        /// <summary>
        /// 并行数量
        /// </summary>
        const int Psize = 10000;
       
        public BerkeleyDBHelper(DBType dBType=DBType.BTree)
        {
            this.DBType = dBType;
            Init();
        }

        /// <summary>
        /// 重新初始化
        /// </summary>
        public void Reset()
        {
            Init();
        }

        #region Put 
        public void Put(TKey key,TValue  value, Transaction txn=null)
        {
            byte[] kdata= SerializerFactory<CommonSerializer>.Serializer(key);
            byte[] vdata = SerializerFactory<CommonSerializer>.Serializer(value);
            Put(kdata, vdata,txn);
        }
        public void PutNoOverwrite(TKey key, TValue value,Transaction txn)
        {
            byte[] kdata = SerializerFactory<CommonSerializer>.Serializer(key);
            byte[] vdata = SerializerFactory<CommonSerializer>.Serializer(value);
            PutNoOverwrite(kdata, vdata,txn);
        }
        public void Put(Dictionary<TKey,TValue> data)
        {
            Transaction txn = GetTransaction();
            if (data.Count > Psize)
            {
                Parallel.ForEach(data, (kv) =>
                {
                    Put(kv.Key, kv.Value, txn);
                });
            }
            else
            {
                foreach (KeyValuePair<TKey, TValue> kv in data)
                {
                    Put(kv.Key, kv.Value, txn);
                }
            }
            Commit(txn);
        }

        #endregion

        #region Get
        public TValue GetData(TKey key,Transaction txn=null)
        {
            TValue value = default(TValue);
            byte[] kdata = SerializerFactory<CommonSerializer>.Serializer(key);
            byte[] vdata= Get(kdata,null,txn);
            if (null != vdata)
            {
                value= SerializerFactory<CommonSerializer>.Deserialize<TValue>(vdata);
            }
            return value;
        }

        public Dictionary<TKey, TValue> Get(HashSet<TKey> keys)
        {
            List<byte[]> lstKey = new List<byte[]>(keys.Count);
            Dictionary<TKey, TValue> result = new Dictionary<TKey, TValue>(keys.Count);
          
            if (keys.Count > Psize)
            {
                Transaction txn = GetTransaction();
                Parallel.ForEach(keys, (p) =>
                {
                    result[p] = GetData(p,txn);
                });
                Commit(txn);
               
            }
            else
            {
                foreach (TKey key in keys)
                {
                    result[key]= GetData(key);
                }
               
            }
            return result;
        }

        #endregion

        #region Delete
        public void Delete(TKey key,Transaction txn=null)
        {
            byte[] kdata = SerializerFactory<CommonSerializer>.Serializer(key);
            Delete(kdata,txn);
        }
        public void Delete(TKey[] keys)
        {
          
            //
            if (keys.Length>Psize)
            {
                Transaction txn = GetTransaction();
                Parallel.ForEach(keys, (p) =>
                {
                    Delete(p,txn);
                });
                Commit(txn);
            }
            else
            {
                foreach(TKey key in keys)
                {
                    Delete(key,null);
                }
            }
            //
        }

        public void DeleteRang(TKey[] keys)
        {
            //
            ConcurrentBag<byte[]> bag = new ConcurrentBag<byte[]>();
            if (keys.Length > Psize)
            {

                Parallel.ForEach(keys, (p) =>
                {
                    byte[] kdata = SerializerFactory<CommonSerializer>.Serializer(p);
                    bag.Add(kdata);
                });
            }
            else
            {
                foreach (TKey key in keys)
                {
                    byte[] kdata = SerializerFactory<CommonSerializer>.Serializer(key);
                    bag.Add(kdata);
                }
            }
            List<byte[]> lst = new List<byte[]>(bag.ToArray());
            Delete(lst);
            lst.Clear();
            bag = null;
        }
        #endregion
    }
}
