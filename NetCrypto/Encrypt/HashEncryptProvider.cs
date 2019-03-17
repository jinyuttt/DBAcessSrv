#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto.Encrypt
* 项目描述 ：
* 类 名 称 ：Irreversible
* 类 描 述 ：
* 命名空间 ：NetCrypto.Encrypt
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/6 21:48:07
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NetCrypto.Encrypt
{
    /* ============================================================================== 
* 功能描述：Irreversible  不可逆加密SHA,MD5   
* MD5输出128bit/16字节
SHA1输出160bit/20字节
SHA256输出256bit/32字节
SHA244输出244bit/30.5
SHA512输出512bit/64字节
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class HashEncryptProvider : IDataEncrypt
    {
        static readonly Dictionary<string, string> HashName = new Dictionary<string, string> {
            {"SHA1",null},{"MD5",null},{"SHA256",null},
            { "SHA-256",null},{ "SHA384",null},{"SHA-384",null},
            { "SHA-384",null},{ "SHA512",null},{"SHA-512",null }
        };

        public byte[] Encrypt(Stream msg,string name=null)
        {
            if(string.IsNullOrEmpty(name)|| !HashName.ContainsKey(name))
            {
                name = "SHA1";
            }
            HashAlgorithm algorithm = HashAlgorithm.Create(name);
            byte[] myHash = algorithm.ComputeHash(msg);
            return myHash;
        }
      
        public string Encrypt(string msg, string key = null)
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(msg));
            var myHash = Encrypt(stream, key);
            //返回字符串
            var enText = new StringBuilder();
            foreach (byte iByte in myHash)
            {
                enText.AppendFormat("{0:x2}", iByte);
            }
            return enText.ToString();
        }

        public byte[] Encrypt(byte[] msg, string key = null)
        {
            MemoryStream stream = new MemoryStream(msg);
            var myHash = Encrypt(stream, key);
            //返回字符串
            return myHash;
        }
    }
}
