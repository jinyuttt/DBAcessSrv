using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NetCrypto
{
    //私钥   AES固定格式为128/192/256 bits.即：16/24/32bytes
    //初始化向量参数，AES 为16bytes.
    public class AesEncryptProvider : IDataEncrypt
    {
        private const int BufferSize = 1024;

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Encrypt(string msg, string key = null)
        {
            if (string.IsNullOrEmpty(msg)) return null;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(msg);
            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray);
        }

        /// <summary>
        ///  AES加密
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] msg, string key = null)
        {
            if(msg==null)
            {
                return null;
            }
            byte[] cryptograph = null; // 加密后的密文  
            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            // 开辟一块内存流 ,存储密文数据
            using (MemoryStream memData = new MemoryStream(msg))
            {
                using (MemoryStream Memory = new MemoryStream())
                {
                    // 把内存流对象包装成加密流对象  
                    using (CryptoStream Encryptor = new CryptoStream(Memory, cTransform, CryptoStreamMode.Write))
                    {
                        int bytesRead = 0;
                        byte[] buffer = new byte[BufferSize];
                        do
                        {
                            bytesRead = memData.Read(buffer, 0, BufferSize);
                            Encryptor.Write(buffer, 0, bytesRead);
                        } while (bytesRead > 0);
                        Encryptor.FlushFinalBlock();
                        //// 明文数据写入加密流  
                        //Encryptor.Write(msg, 0, msg.Length);
                        //Encryptor.FlushFinalBlock();
                        cryptograph = Memory.ToArray();
                    }
                }
            }
            return cryptograph;

          
        }

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] Encrypt(Stream msg, string key = null)
        {
            byte[] buf = new byte[msg.Length];
            msg.Read(null, 0, buf.Length);
            return Encrypt(buf, key);
        }
    }
}
