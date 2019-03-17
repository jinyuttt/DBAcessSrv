#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto
* 项目描述 ：
* 类 名 称 ：ManagerKeys
* 类 描 述 ：
* 命名空间 ：NetCrypto
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/6 23:54:28
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

namespace NetCrypto
{
    /* ============================================================================== 
* 功能描述：ManagerKeys 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class ManagerKeys
    {
         
        private static Lazy<ManagerKeys> instance = new Lazy<ManagerKeys>();
        public static ManagerKeys Singleton { get { return instance.Value; } }

        private Dictionary<string, RSAKey> dicKeys = new Dictionary<string, RSAKey>();
        private const string defName = "RSA";
        private const int RsaKeySize = 2048;
        private const string publicKeyFileName = "RSA.Pub";
        private const string privateKeyFileName = "RSA.Private";

        private RSAKey rSAKey;
        public RSAKey GetKey(string name=null)
        {
            if(string.IsNullOrEmpty(name))
            {
                name = defName;
            }
            RSAKey rSAKey;
            dicKeys.TryGetValue(name, out rSAKey);
            return rSAKey;
        }
        public void SetKey(string name,RSAKey key)
        {
            if(string.IsNullOrEmpty(name))
            {
                name = defName;
            }
            dicKeys[name]= key;
        }
      

        /// <summary>
        ///在给定路径中生成XML格式的私钥和公钥。
        /// </summary>
        public void GenerateKeys(string path)
        {
           // if (!File.Exists(Path.Combine(path, publicKeyFileName)) || !File.Exists(Path.Combine(path, privateKeyFileName)))
            {
                using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
                //using(var rsa=RSA.Create())
                {
                    try
                    {
                        // var ss = rsa.ToXmlString(false);
                        // var kk = rsa.ToXmlString(true);
                        // 获取私钥和公钥。
                       
                        byte[] publicKey = rsa.ExportCspBlob(false);
                        byte[] privateKey =rsa.ExportCspBlob(true);
                       
                        // 保存到磁盘
                        //if (!File.Exists(Path.Combine(path, publicKeyFileName)))
                        {
                            File.WriteAllBytes(Path.Combine(path, publicKeyFileName), publicKey);
                        }
                       // if (!File.Exists(Path.Combine(path, privateKeyFileName)))
                        {
                            File.WriteAllBytes(Path.Combine(path, privateKeyFileName), privateKey);
                        }
                        rSAKey.PrivateKey =Convert.ToBase64String(privateKey);
                        rSAKey.PublicKey = Convert.ToBase64String(publicKey);
                    
                        //Console.WriteLine(string.Format("生成的RSA密钥的路径: {0}\\ [{1}, {2}]", path, publicKeyFileName, privateKeyFileName));
                    }
                    finally
                    {
                      //  rsa.PersistKeyInCsp = false;
                    }
                }
            }
        }

        /// <summary>
        /// 存储私钥
        /// </summary>
        /// <param name="path"></param>
        public void SaveLocal(string path,string xml)
        {
           
                using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
                {
                try
                {
                    rsa.ImportCspBlob(Convert.FromBase64String(xml));
                    // 获取私钥和公钥。
                    var publicKey = rsa.ExportCspBlob(false);
                  
                
                    // 保存到磁盘
                    if (!rsa.PublicOnly)
                    {
                            var privateKey = rsa.ExportCspBlob(true);
                            File.WriteAllBytes(Path.Combine(path, publicKeyFileName), publicKey);
                            File.WriteAllBytes(Path.Combine(path, privateKeyFileName), privateKey);
                    }
                    else
                    {
                        File.WriteAllBytes(Path.Combine(path, publicKeyFileName), publicKey);
                    }
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
                }
            
        }

        public RSAKey GetRSAKey(string path)
        {
            RSAKey key=new RSAKey();
            if (File.Exists(Path.Combine(path, publicKeyFileName)))
            {
                key.PublicKey =Convert.ToBase64String(File.ReadAllBytes(Path.Combine(path, publicKeyFileName)));
            }
            if (File.Exists(Path.Combine(path, privateKeyFileName)))
            {
                key.PrivateKey = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(path, privateKeyFileName)));
            }
            return key;
        }

       
    }
    public struct RSAKey
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }
}
