using System;
using System.Collections.Generic;
using System.Text;

namespace DBQuery
{

    /// <summary>
    ///文件导入接口
    /// </summary>
   public interface IDBFile
    {
        void Start();

        void Stop();
    }
}
