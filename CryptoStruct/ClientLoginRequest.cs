#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：CryptoStruct
* 项目描述 ：
* 类 名 称 ：Class1
* 类 描 述 ：
* 命名空间 ：CryptoStruct
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/9 1:05:27
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System.Runtime.InteropServices;

namespace CryptoStruct
{
    /* ============================================================================== 
* 功能描述：ClientRequest 客户端请求登录信息并且获取RSA秘钥
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    [StructLayout(LayoutKind.Sequential,CharSet =CharSet.Unicode)]
    public struct ClientLoginRequest
    {
        /// <summary>
        /// 内部版本号
        /// </summary>
        public int Version;

        /// <summary>
        /// 授权方式，最多支持25
        /// 0默认授权，1文件授权
        /// </summary>
        public byte Authorization;

        /// <summary>
        /// 发送请求的时间
        /// </summary>
        public long ReqTime;

        /// <summary>
        /// 是否有授权时间限制
        /// </summary>
        public byte Limit;

        /// <summary>
        /// 最后一次允许请求的时间
        /// </summary>
        public long LastTime;

        [MarshalAs(UnmanagedType.ByValTStr,SizeConst =42)]
        /// <summary>
        /// 请求验证码
        /// </summary>
        public string HashCode;

    }
}
