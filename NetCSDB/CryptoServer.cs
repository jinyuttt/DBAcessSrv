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
using NetCrypto.RSACrypto;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace NetCSDB
{
    /* ============================================================================== 
* 功能描述：CryptoServer 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class CryptoServer
    {
        static long Sessionid = 0;
        RsaSrvDecryptProvider rsaSrvDecrypt = new RsaSrvDecryptProvider();
        private static Lazy<CryptoServer> instance = new Lazy<CryptoServer>();
        AesDecryptProvider decryptProvider = new AesDecryptProvider();
        public static CryptoServer Singleton
        {
            get { return instance.Value; }
        }

        public CryptoServer()
        {
            RSAParametersKey.CreateKeys(AppDomain.CurrentDomain.BaseDirectory);
            var rsaKeys = RSAParametersKey.GetRSAKey(AppDomain.CurrentDomain.BaseDirectory);
            //
            CipherReply.Singleton.RSAPublicKeys = rsaKeys.PublicKey;
            CipherReply.Singleton.RSAPrivateKeys = rsaKeys.PrivateKey;
            //
            SrvSetting.IsAuthorization = true;
        }

        /// <summary>
        /// 接收登录
        /// </summary>
        /// <param name="request"></param>
        /// <param name="host"></param>
        private bool AuthorizationCheck(ClientLoginRequest request)
        {
            //先验证数据
            HashEncryptProvider provider = new HashEncryptProvider();
            if (request.Authorization==0&&SrvSetting.IsAuthorization)
            {
                //验证默认授权（CryptoStruct库必须一致）
               
                var code= provider.Encrypt(CipherReply.RequestInfo);
                if(code==request.HashCode)
                {
                    //验证通过
                    return true;
                }
                return false;
            }
            else if(request.Authorization==1&&SrvSetting.IsFileauthorization)
            {
                FileStream fs = new FileStream(SrvSetting.AuthorizationFile, FileMode.Open, FileAccess.Read);
                var result = provider.Encrypt(fs);
                var code = Convert.ToBase64String(result);
                if (code == request.HashCode)
                {
                    //验证通过
                    return true;
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// 处理登录验证
        /// </summary>
        /// <param name="request"></param>
        /// <param name="host"></param>
        public ServerResponse ResponseLogin(ClientLoginRequest request, string host)
        {
            if(AuthorizationCheck(request))
            {
                //
                ServerResponse response = new ServerResponse();
                response.Clientid = Interlocked.Increment(ref Sessionid);
                response.RSAPublicKeys = CipherReply.Singleton.RSAPublicKeys;
                response.IsSucess = true;
                //构造结构返回
                CipherReply.Singleton.Session[Sessionid] = DateTime.Now;
             

                StringBuilder sbr = new StringBuilder();
                sbr.Append("客户端请求登陆验证成功");
                sbr.AppendFormat("客户端版本:{0}", request.Version);
                sbr.AppendFormat("客户端授权方式:{0}", request.Authorization);
                sbr.AppendFormat("客户端地址:{0}", host);
                //
                Console.WriteLine(sbr.ToString());//日志接口
                return response;


            }
            else
            {
                StringBuilder sbr = new StringBuilder();
                sbr.Append("客户端请求登陆验证失败");
                sbr.AppendFormat("客户端版本:{0}", request.Version);
                sbr.AppendFormat("客户端授权方式:{0}", request.Authorization);
                sbr.AppendFormat("客户端地址:{0}", host);
                //
                Console.WriteLine(sbr.ToString());//日志接口
                ServerResponse response = new ServerResponse();
                response.IsSucess = false;
                return response;
            }
            //构造网络回传结构
            
        }

        /// <summary>
        /// 处理请求;解密出来请求数据
        /// </summary>
        /// <param name="request">解析数据</param>
        /// <param name="rsqKey">RSA私钥</param>
        /// <returns></returns>
        public ClientRequest ProcessRequest(byte[] request)
        {
            //处理一般请求
            //业务数据
            //拿到AESKey
            //按照客户端结构
            int len = BitConverter.ToInt32(request, 0);
            int aesKeyLen = BitConverter.ToInt32(request, 4);
            long sessionid = BitConverter.ToInt64(request, 8);
            long taskID = BitConverter.ToInt64(request, 16);
            byte[] reqData = new byte[len - aesKeyLen];
            string aesKey = "";
            using (var mem =new MemoryStream(request))
            {
                mem.Position = 24;
                mem.Read(reqData, 0, reqData.Length);//读取加密的数据
                //
                byte[] aes = new byte[aesKeyLen];
                mem.Read(aes, 0, aes.Length);
                // aesKey =Encoding.UTF8.GetString(aes);
                aesKey = Convert.ToBase64String(aes);
            }
            //
            ClientRequest clientReq= new ClientRequest();
            clientReq.AESKey = rsaSrvDecrypt.Decrypt(aesKey, CipherReply.Singleton.RSAPrivateKeys);//解密AES秘钥
            clientReq.SessionID = sessionid;
            clientReq.TaskID = taskID;
            clientReq.data = decryptProvider.Decrypt(reqData, clientReq.AESKey);//用AES解密数据
            return clientReq;
           
        }

        /// <summary>
        ///  AES加密
        /// </summary>
        /// <param name="reslut">待加密数据</param>
        /// <param name="aesKey">AES秘钥</param>
        /// <returns></returns>
        public byte[] EnCrypto(byte[]reslut,string aesKey)
        {
            AesEncryptProvider aesEncrypt = new AesEncryptProvider();
            var nbytes = aesEncrypt.Encrypt(reslut, aesKey);
            return nbytes;
        }

        
    }

   
}
