#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：CryptoStruct
* 项目描述 ：
* 类 名 称 ：ClientRequest
* 类 描 述 ：
* 命名空间 ：CryptoStruct
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/9 15:10:05
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
* 功能描述：ClientRequest 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/


    [StructLayout(LayoutKind.Sequential)]
    public struct ClientRequest
    {
        //传输：数据总长， AES秘钥长度，sessionid，taskID
        public long SessionID;
        public long TaskID;
        public byte[] data;
        public string AESKey;
    }
}
