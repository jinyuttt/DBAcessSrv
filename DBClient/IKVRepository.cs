using System.Collections.Generic;

namespace DBClient
{
    /// <summary>
    /// NoSQL操作接口
    /// </summary>
    interface IKVRepository
    {
        void Put<TKey,TValue>(TKey key, TValue value);

        void Put<TKey, TValue>(Dictionary<TKey, TValue> kv);

        void Delete<T>(T key);

        void Delete<T>(List<T> list);

        TValue GetValue<TKey, TValue>(TKey key);

        Dictionary<TKey, TValue> GetValue<TKey, TValue>(List<TKey> lst);

        void Clear();
    }
}
