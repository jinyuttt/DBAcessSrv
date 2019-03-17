#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto.Encrypt
* 项目描述 ：
* 类 名 称 ：RsaSrvEncryptProvider
* 类 描 述 ：
* 命名空间 ：NetCrypto.Encrypt
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/7 2:55:15
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

namespace NetCrypto.Encrypt
{
    /* ============================================================================== 
* 功能描述：RsaSrvEncryptProvider 1024位的证书，加密时最大支持117个字节，解密时为128；2048位的证书，加密时最大支持245个字节，解密时为256。
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class RsaSrvEncryptProvider : IDataEncrypt
    {
     

        public string Encrypt(string msg, string key = null)
        {
            try
            {
                 var result=  Encrypt(Encoding.UTF8.GetBytes(msg),key);
                 return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] Encrypt(byte[] msg, string key = null)
        {
            try
            {
                byte[] encryptedData;
                int blockSize = 0;
                using (var rsa = RSA.Create())
                {
                  
                    blockSize= RSAParametersKey.GetMaxBlockSize(rsa, RSAEncryptionPadding.Pkcs1);
                    RSAParametersKey.SetRSAKeys(rsa, key);
                   
                    if (msg.Length <= blockSize)
                          return rsa.Encrypt(msg, RSAEncryptionPadding.Pkcs1);
                    using (var plaiStream = new MemoryStream(msg))
                    {
                        using (var crypStream = new MemoryStream())
                        {
                            var offSet = 0;
                            var inputLen = msg.Length;//总长
                            //循环
                            for (int i = 0; inputLen - offSet > 0; offSet = i * blockSize)
                            {
                                //剩余数量足够
                                if (inputLen - offSet > blockSize)
                                {
                                    var buffer = new Byte[blockSize];
                                    plaiStream.Read(buffer, 0, blockSize);//读取到数组中
                                    var reslut = rsa.Encrypt(buffer, RSAEncryptionPadding.Pkcs1);
                                       crypStream.Write(reslut, 0, reslut.Length);
                                }
                                else
                                {
                                    var buffer = new byte[inputLen - offSet];
                                    plaiStream.Read(buffer, 0, buffer.Length);
                                    var reslut = rsa.Encrypt(buffer, RSAEncryptionPadding.Pkcs1);
                                    crypStream.Write(reslut, 0, reslut.Length);
                                }
                                ++i;
                            }
                            crypStream.Position = 0;
                            encryptedData = crypStream.ToArray();
                        }
                    }
                }
                return encryptedData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] Encrypt(Stream msg, string key = null)
        {
            try
            {
                byte[] encryptedData;
                int blockSize = 0;
                using (var rsa = RSA.Create())
                {
                    blockSize = RSAParametersKey.GetMaxBlockSize(rsa, RSAEncryptionPadding.Pkcs1);
                    RSAParametersKey.SetRSAKeys(rsa, key);

                    if (msg.Length <= blockSize)
                    {
                        var buffer = new Byte[msg.Length];
                        msg.Read(buffer, 0, buffer.Length);
                        return rsa.Encrypt(buffer, RSAEncryptionPadding.Pkcs1);
                    }
                    using (var plaiStream = new BufferedStream(msg))
                    {
                        using (var crypStream = new MemoryStream())
                        {
                            var offSet = 0;
                            var inputLen = msg.Length;//总长
                            //循环
                            for (int i = 0; inputLen - offSet > 0; offSet = i * blockSize)
                            {
                                //剩余数量足够
                                if (inputLen - offSet > blockSize)
                                {
                                    var buffer = new Byte[blockSize];
                                    plaiStream.Read(buffer, 0, blockSize);//读取到数组中
                                    var reslut = rsa.Encrypt(buffer, RSAEncryptionPadding.Pkcs1);
                                    crypStream.Write(reslut, 0, reslut.Length);
                                }
                                else
                                {
                                    var buffer = new byte[inputLen - offSet];
                                    plaiStream.Read(buffer, 0, buffer.Length);
                                    var reslut = rsa.Encrypt(buffer, RSAEncryptionPadding.Pkcs1);
                                    crypStream.Write(reslut, 0, reslut.Length);
                                }
                                ++i;
                            }
                            crypStream.Position = 0;
                            encryptedData = crypStream.ToArray();
                        }
                    }
                }
                return encryptedData;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
