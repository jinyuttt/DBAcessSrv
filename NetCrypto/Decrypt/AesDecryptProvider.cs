using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NetCrypto.Decrypt
{
    public class AesDecryptProvider : IDataDecrypt
    {
        private const int BufferSize = 1024;
        public string Decrypt(string msg, string sKey = null)
        {
            if (string.IsNullOrEmpty(msg)) return null;
            Byte[] toEncryptArray = Convert.FromBase64String(msg);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(sKey),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }

        public byte[] Decrypt(byte[] msg, string sKey = null)
        {
            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(sKey),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            byte[] original = null; // 解密后的明文  
            // 开辟一块内存流，存储密文  
            using (MemoryStream Memory = new MemoryStream(msg))
            {
                // 把内存流对象包装成加密流对象  
                using (CryptoStream Decryptor = new CryptoStream(Memory, cTransform, CryptoStreamMode.Read))
                {
                    // 明文存储区  
                    using (MemoryStream originalMemory = new MemoryStream())
                    {
                        byte[] Buffer = new byte[BufferSize];
                        int readBytes = 0;
                        while ((readBytes = Decryptor.Read(Buffer, 0, BufferSize)) > 0)
                        {
                            originalMemory.Write(Buffer, 0, readBytes);
                        }
                        original = originalMemory.ToArray();
                    }
                }
            }
            return original;
          
        }
    }
}
