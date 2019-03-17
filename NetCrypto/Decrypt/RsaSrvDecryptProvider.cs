#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto.Decrypt
* 项目描述 ：
* 类 名 称 ：RsaSrvDecryptProvider
* 类 描 述 ：
* 命名空间 ：NetCrypto.Decrypt
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/8 3:00:54
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using NetCrypto.RSACrypto;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NetCrypto.Decrypt
{
    /* ============================================================================== 
* 功能描述：RsaSrvDecryptProvider 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class RsaSrvDecryptProvider : IDataDecrypt
    {
        public string Decrypt(string msg, string sKey = null)
        {
            var result = Decrypt(Convert.FromBase64String(msg), sKey);
            return  Encoding.UTF8.GetString(result);
        }

        public byte[] Decrypt(byte[] msg, string sKey = null)
        {
            byte[] decryptedData;
            using (var rsa = RSA.Create())
            {
                int blockSize = RSAParametersKey.GetMaxBlockSize(rsa, RSAEncryptionPadding.Pkcs1)+11;
                RSAParametersKey.SetRSAKeys(rsa, sKey);
                using (var plaiStream = new MemoryStream(msg))
                {
                    using (var decrypStream = new MemoryStream())
                    {
                        var offSet = 0;
                        var inputLen = msg.Length;
                        for (var i = 0; inputLen - offSet > 0; offSet = i * blockSize)
                        {
                            if (inputLen - offSet > blockSize)
                            {
                                 var buffer = new byte[blockSize];
                                 plaiStream.Read(buffer, 0, blockSize);//读取密文
                                 var decrypData = rsa.Decrypt(buffer,RSAEncryptionPadding.Pkcs1);//解密
                                 decrypStream.Write(decrypData, 0, decrypData.Length);
                            }
                            else
                            {
                                var buffer = new byte[inputLen - offSet];
                                plaiStream.Read(buffer, 0, buffer.Length);
                                var decrypData = rsa.Decrypt(buffer, RSAEncryptionPadding.Pkcs1);
                                decrypStream.Write(decrypData, 0, decrypData.Length);
                            }
                            ++i;
                        }
                        decrypStream.Position = 0;
                        decryptedData = decrypStream.ToArray();
                    }
                }
            }
            return decryptedData;
        }
    }
}
