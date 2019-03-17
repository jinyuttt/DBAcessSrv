#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCSDB
* 项目描述 ：
* 类 名 称 ：NettySrvManager
* 类 描 述 ：
* 命名空间 ：NetCSDB
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/15 19:04:07
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


namespace NettyTransmission
{
    public class NettyAddress
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string Flage { get; set; }

        public override bool Equals(object obj)
        {
            NettyAddress addr = obj as NettyAddress;
            if(addr==null)
            {
                return false;
            }
            if (this.Host == addr.Host && this.Port == addr.Port)
            {
                return true;
            }
            else
            {
                return false;
            }
           
        }

        public override int GetHashCode()
        {
            return this.Host.GetHashCode() + this.Port.GetHashCode();
        }
    }
}
