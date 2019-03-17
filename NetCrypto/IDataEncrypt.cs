using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NetCrypto
{

    /// <summary>
    /// 加密接口
    /// </summary>
    public interface IDataEncrypt
    {
        string Encrypt(string msg,string key=null);
        byte[] Encrypt(byte[] msg,string key=null);

        byte[] Encrypt(Stream  msg, string key = null);
    }
}
