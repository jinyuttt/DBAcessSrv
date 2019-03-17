#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：CryptoStruct
* 项目描述 ：
* 类 名 称 ：CipherReply
* 类 描 述 ：
* 命名空间 ：CryptoStruct
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/9 1:49:32
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CryptoStruct
{
    /* ============================================================================== 
* 功能描述：CipherReply 接收的数据,一些信息
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class CipherReply
    {
        private static Lazy<CipherReply> instance = new Lazy<CipherReply>();
        public static CipherReply Singleton
        {
            get { return instance.Value; }
        }
        public const string RequestInfo = "now request publicKey";

        public CipherReply()
        {
            Session = new ConcurrentDictionary<long, DateTime>();
        }
        /// <summary>
        /// 客户端使用的AES秘钥
        /// </summary>
        public string AESKeys { get; set; }

        /// <summary>
        /// 服务端使用的RSA私钥
        /// </summary>
        public string RSAPrivateKeys { get; set; }

        /// <summary>
        /// 服务端使用的公钥
        /// </summary>
        public string RSAPublicKeys { get; set; }


        /// <summary>
        /// 服务端使用的验证码
        /// </summary>
        public string RspCode { get; set; }

        /// <summary>
        /// 客户端信息，强制每天登陆
        /// </summary>
        public IDictionary<long, DateTime> Session{get;set;}

        /// <summary>
        /// 当前版本信息，是CryptoStruct定义的
        /// </summary>
        public int Version = 1;
         
    }
}
