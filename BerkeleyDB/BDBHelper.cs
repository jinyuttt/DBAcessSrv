using System;
using System.Collections.Generic;
using System.Text;

namespace BDB
{
    public class BDBHelper<TKey, TValue> : BerkeleyDBHelper<TKey, TValue>
    {
        public BDBHelper(DBType dBType=DBType.BTree) : base(dBType)
        {

        }
        public TValue GetValue(TKey key)
        {
            return GetData(key);
        }

        public Dictionary<TKey, TValue> GetKVS(HashSet<TKey> keys)
        {
            return Get(keys);
        }

        public void PutKVS(Dictionary<TKey, TValue> data)
        {
            Put(data);
        }
        public void PutNoOverwrite(TKey key, TValue value)
        {
            PutNoOverwrite(key, value);
        }
        public void PutValue(TKey key, TValue value)
        {
            Put(key, value);
        }
        public void DeleteValue(TKey key)
        {
            Delete(key);
        }

        public void DeleteList(TKey[]keys)
        {
            Delete(keys);
        }

       
    }
    }
