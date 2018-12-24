using System;
using System.Collections.Generic;
using System.Text;

namespace DBQuery
{
   public interface IKVDB<TKey,TValue>
    {
        void Put(TKey key, TValue value, int cacheTime=0);

        TValue Get(TKey key);

        void Delete(TKey key);

        void Clear();
    }
}
