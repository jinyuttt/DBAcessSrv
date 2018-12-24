using System;
using System.Collections.Generic;
using System.Text;

namespace DBQuery
{

    /// <summary>
    /// 内存数据接口
    /// </summary>
  public  interface IDBMemory
    {
        void Start();

        void Stop();

        void Clear();

        void Close();
    }
}
