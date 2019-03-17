using System;
using System.Collections.Generic;
using System.Text;

namespace NetCrypto
{

    /// <summary>
    /// 解密接口
    /// </summary>
    interface IDataDecrypt
    {
        string Decrypt(string msg, string sKey=null);
        byte[] Decrypt(byte[] msg, string sKey=null);
    }
}
