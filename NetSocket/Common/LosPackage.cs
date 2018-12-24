/**
* 命名空间: NetSocket 
* 类 名：LosPackage 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：LosPackage 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
   public class LosPackage:UDPPackage
    {
      public  byte[] PData = new byte[13];
      
        public LosPackage(byte[]data)
        {
            PData = data;
           if(data.Length>12)
            {
                this.packageType = data[0];
                this.packageID = BitConverter.ToInt64(data, 1);
                this.packageSeq = BitConverter.ToInt32(data, 9);
            }
        }
        public LosPackage()
        {

        }
        public void Pack()
        {
            PData[0] = this.packageType;
            Array.Copy(BitConverter.GetBytes(this.packageID), 0, PData, 1, 8);
            Array.Copy(BitConverter.GetBytes(this.packageSeq), 0, PData, 9, 4);
        }
    }
}
