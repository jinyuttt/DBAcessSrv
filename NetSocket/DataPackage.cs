/**
* 命名空间: NetSocket 
* 类 名：DataPackage 
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
    /// 功能描述    ：DataPackage 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
   public class UDPDataPackage:UDPPackage
    {
        /// <summary>
        /// 大包总长
        /// </summary>
        public long packageSum= 0;

       

        /// <summary>
        /// 本包长度
        /// </summary>
        public short pacakeLen = 0;

        /// <summary>
        /// 数据区
        /// </summary>
        public byte[] data = null;


        /// <summary>
        /// 组包后的数据
        /// </summary>
        public byte[] packData = null;

       /// <summary>
       /// 数据区的偏移
       /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 数据区的长度
        /// </summary>
        public int DataLen { get; set; }

        public int PackageNum { get; set; }

        public const int HeadLen = 35;

        public byte[] Head=null;

        public UDPDataPackage()
        {
            this.packageType = 0;
            Head = new byte[HeadLen];
        }

        public  void Pack()
        {
           // 1字节数据类型 + 8字节通信标识 + 8字节包ID + 8字节总长 + 4字节包序列 + 2字节本包长 + 数据区
            packData = new byte[HeadLen + data.Length];
            Head[0] = packageType;
            Array.Copy(BitConverter.GetBytes(socketID), 0, Head, 1, 8);
            Array.Copy(BitConverter.GetBytes(packageID), 0, Head, 9, 8);
            Array.Copy(BitConverter.GetBytes(packageSum), 0, Head, 17, 8);
            Array.Copy(BitConverter.GetBytes(packageSeq), 0, Head, 25, 4);
            Array.Copy(BitConverter.GetBytes(pacakeLen), 0, Head, 29, 2);
            Array.Copy(BitConverter.GetBytes(PackageNum), 0, Head, 31, 4);
            //
            Array.Copy(Head, packData, HeadLen);
            Array.Copy(data, 0, packData,HeadLen,data.Length);

        }

        /// <summary>
        /// 组包
        /// </summary>
        /// <param name="buf">组包区</param>
        /// <param name="offset">组包区偏移</param>
        /// <param name="len">组包区长度</param>
        public void Pack(byte[]buf,int offset,int len)
        {
            // 1字节数据类型 + 8字节通信标识 + 8字节包ID + 8字节总长 + 4字节包序列 + 2字节本包长 + 数据区
            if (DataLen == 0)
            {
                DataLen = data.Length;
            }
            int curLen = DataLen>len - HeadLen ? len - HeadLen : DataLen;
            pacakeLen = (short)curLen;
            packData = buf;
            Head[0] = packageType;
            this.packageSeq++;
            Array.Copy(BitConverter.GetBytes(socketID), 0, Head, 1, 8);
            Array.Copy(BitConverter.GetBytes(packageID), 0, Head, 9, 8);
            Array.Copy(BitConverter.GetBytes(packageSum), 0, Head, 17, 8);
            Array.Copy(BitConverter.GetBytes(packageSeq), 0, Head, 25, 4);
            Array.Copy(BitConverter.GetBytes(pacakeLen), 0, Head, 29, 2);
            Array.Copy(BitConverter.GetBytes(PackageNum), 0, Head, 31, 4);
            //
            Array.Copy(Head,0, packData,offset, HeadLen);
           
            if(DataLen > len-HeadLen)
            {
                //数据长度大于buf长度可装长度
                Array.Copy(data, Offset, packData, offset + HeadLen, len-HeadLen);
            }
            else
            {
                Array.Copy(data, Offset, packData, offset + HeadLen, DataLen);
            }
          

        }

        public void UnPack(byte[] pData,int offset=0,int len=0)
        {
            if(len==0)
            {
                len = pData.Length;
            }
            if(len<HeadLen)
            {
                return;
            }
            packData = pData;
            data = new byte[len - HeadLen];
            Array.Copy(packData,offset, Head,0, HeadLen);
            Array.Copy(packData,offset+ HeadLen, data, 0, data.Length);
            //
            socketID = BitConverter.ToInt64(Head, 1);
            packageID = BitConverter.ToInt64(Head, 9);
            packageSum= BitConverter.ToInt64(Head, 17);
            packageSeq = BitConverter.ToInt32(Head, 25);
            pacakeLen= BitConverter.ToInt16(Head, 29);
            PackageNum= BitConverter.ToInt32(Head, 31);

        }
    }
}
