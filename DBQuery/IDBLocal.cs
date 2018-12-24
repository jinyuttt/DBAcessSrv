using System;
using System.Collections.Generic;
using System.Text;

namespace DBQuery
{
  public  interface IDBLocal
    {

        void Start();

        void Stop();

        void Clear();

        void Close();
    }
}
