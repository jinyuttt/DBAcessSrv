#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto
* 项目描述 ：
* 类 名 称 ：EncryptProviderKey
* 类 描 述 ：
* 命名空间 ：NetCrypto
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/6 23:34:50
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Text;

namespace NetCrypto
{
    /* ============================================================================== 
* 功能描述：EncryptProviderKey 随机生成Key
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class EncryptProviderKey
    {
        public static string GetKeys(int size = 128)
        {
            int left = size / 8;//把位转换成字节
            int count = left;
            byte[] buf = new byte[left];
            while (left > 0)
            {
                string key = Guid.NewGuid().ToString("N");
                byte[] tmp = Encoding.UTF8.GetBytes(key);
                int len = tmp.Length > left ? left : tmp.Length;
                Array.Copy(tmp, 0, buf, count - left, len);
                left = left - len;
            }
            return Encoding.UTF8.GetString(buf);
        }
    }
}
