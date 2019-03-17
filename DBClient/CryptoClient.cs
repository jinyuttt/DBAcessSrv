#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto
* 项目描述 ：
* 类 名 称 ：CryptoServer
* 类 描 述 ：
* 命名空间 ：NetCrypto
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/8 23:51:41
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using CryptoStruct;
using NetCrypto;
using NetCrypto.Decrypt;
using NetCrypto.Encrypt;
using Serializer;
using System;
using System.IO;
using System.Timers;

namespace DBClient
{
    /* ============================================================================== 
* 功能描述：CryptoClient 处理加密解密 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class CryptoClient
    {
        
        AesEncryptProvider aesEncrypt = new AesEncryptProvider();
        AesDecryptProvider aesDecrypt = new AesDecryptProvider();
        RsaSrvEncryptProvider rsaEncrypt = new RsaSrvEncryptProvider();

        private readonly Timer aesTimer = null;//定时修改AES 

        private static Lazy<CryptoClient> instance = new Lazy<CryptoClient>();

        public static CryptoClient Singleton
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// 服务端分配的ID
        /// </summary>
        public long Sessionid { get; set; }

        /// <summary>
        /// 是否主动登录
        /// </summary>
        public bool IsLogin { get; set; }

        /// <summary>
        /// 构造
        /// </summary>
        public CryptoClient()
        {
            ///获取128位 AES密码
             CipherReply.Singleton.AESKeys=  EncryptProviderKey.GetKeys();
            aesTimer = new Timer
            {
                Enabled = true,
                Interval = 1000 * 60 * 60//1小时
            };
            aesTimer.Elapsed += AesTimer_Elapsed;
            //
            IsLogin = false;
            Sessionid = -1;
        }

        private void AesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //定时更新
            CipherReply.Singleton.AESKeys = EncryptProviderKey.GetKeys();
        }

        /// <summary>
        ///  AES解密
        /// </summary>
        /// <param name="data">待解密内容</param>
        /// <param name="AesKey">AES秘钥</param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] data,string AesKey)
        {
           return aesDecrypt.Decrypt(data, AesKey);
        }

        /// <summary>
        ///  AES加密
        /// </summary>
        /// <param name="reslut">待加密数据</param>
        /// <param name="aesKey">AES秘钥</param>
        /// <returns></returns>
        public byte[] Encrypt(byte[]reslut,string AesKeys)
        {
            Console.WriteLine("AESKEY:" + AesKeys);
            var nbytes = aesEncrypt.Encrypt(reslut, AesKeys);
            return nbytes;
        }

        /// <summary>
        /// 加密AES秘钥
        /// </summary>
        /// <returns></returns>
        public byte[] EncryptAESKey(out string AesKey)
        {

            AesKey = CipherReply.Singleton.AESKeys;
            var nbytes = rsaEncrypt.Encrypt(AesKey, CipherReply.Singleton.RSAPublicKeys);
            return Convert.FromBase64String(nbytes);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <returns></returns>
        public byte[] LoginSrv()
        {
            ClientLoginRequest client = new ClientLoginRequest();
            client.ReqTime = DateTime.Now.Ticks;
            HashEncryptProvider hashEncrypt = new HashEncryptProvider();
            if (ClientSetting.IsFileauthorization)
            {
               
                client.Authorization = 1;
                FileStream fs = new FileStream(ClientSetting.AuthorizationFile, FileMode.Open);
                client.HashCode = Convert.ToBase64String(hashEncrypt.Encrypt(fs));
            }
            else
            {
                client.HashCode =hashEncrypt.Encrypt(CipherReply.RequestInfo);
            }
            //
            byte[] login= StructManager.StructToBytes(client);
           var r=  StructManager.BytesToStruct<ClientLoginRequest>(login);
            //设置标致位
            byte[] req = new byte[login.Length + 1];
            req[0] = 1;
            Array.Copy(login, 0, req, 1, login.Length);
            return req;
        }

        /// <summary>
        /// 设置登录信息
        /// </summary>
        /// <param name="rec"></param>
        public void SetLogin(byte[]rec)
        {
            var response = SerializerFactory<CommonSerializer>.Deserialize<ServerResponse>(rec);
            Sessionid = response.Clientid;
            CipherReply.Singleton.RSAPublicKeys = response.RSAPublicKeys;
            if(response.IsSucess)
            {
                IsLogin = true;
            }
        }

    }
}
