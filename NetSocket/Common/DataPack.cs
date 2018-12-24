/**
* 命名空间: NetSocket 
* 类 名：DataPack 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：DataPack 数据分包
    /// TCP协议：数据头添加4字节int型，标记数据总长度，TCP是一对一的
    /// UDP协议头：1字节数据类型+8字节通信标识+8字节包ID+8字节总长+4字节包序列+2字节本包长+数据区
    ///            1字节回执类型+包ID+包序列
    ///            1字节丢失类型+包ID+包序列
    ///            
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// 说明：这里只管数据分包，其余的定死
    /// </summary>
  public  class DataPack
    {
        public const int TcpPackSize = 1460;
        public const int UdpPackSize = 65535;

        public const int MaxUseBytes = 1024 * 1024 * 1024;//1G
        static CacheManager cacheTCP = null;//缓存采用动态方式
        static CacheManager cacheUDP = null;//缓存采用动态方式
        static UserTokenPool tokenPool = null;
        static long dataPackageid = 0;

        /// <summary>
        /// 直接分包
        /// </summary>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static byte[]  PackTCP(byte[]data,ref int index)
        {

            if(0==index)
            {
                if(data.Length+4<TcpPackSize)
                {
                    byte[] tmp = new byte[data.Length + 4];
                    Array.Copy(BitConverter.GetBytes(data.Length), 0, tmp, 0, 4);
                    Array.Copy(data,0, tmp, 4,data.Length);
                    index += tmp.Length;
                    return tmp;
                }
                else
                {
                    byte[] tmp = new byte[TcpPackSize];
                    Array.Copy(BitConverter.GetBytes(data.Length), 0, tmp, 0, 4);
                    Array.Copy(data, 0, tmp, 4, TcpPackSize-4);
                    index += TcpPackSize - 4;
                    return tmp;
                }
            }
          else  if(index+ TcpPackSize<data.Length)
            {
                byte[] tmp = new byte[TcpPackSize];
                Array.Copy(data, tmp, TcpPackSize);
                index += TcpPackSize;
                return tmp;
            }
            else
            {
                byte[] tmp = new byte[data.Length-index];
                Array.Copy(data, tmp, tmp.Length);
                index += TcpPackSize;
                return tmp;
            }
        }

       /// <summary>
       /// 数据分包，采用缓存
       /// </summary>
       /// <param name="data"></param>
       /// <param name="index"></param>
       /// <returns></returns>
        public static CacheEntity PackEntityTCP(byte[] data, ref int index)
        {
            if(cacheTCP==null)
            {
                cacheTCP = new CacheManager();
                cacheTCP.BufferSize = TcpPackSize;
                cacheTCP.MaxBufferCount = MaxUseBytes / TcpPackSize;
            }
            //
            CacheEntity entity = null;
            if (0 == index)
            {
                if (data.Length + 4 < TcpPackSize)
                {
                    byte[] tmp = new byte[data.Length + 4];
                    Array.Copy(BitConverter.GetBytes(data.Length), 0, tmp, 0, 4);
                    Array.Copy(data, 0, tmp, 4, data.Length);
                    index += tmp.Length;
                    entity = new CacheEntity(tmp,null);
                    return entity;
                }
                else
                {
                    byte[] tmp = null;
                    if (cacheTCP.GetBuffer(out tmp))
                    {
                        entity = new CacheEntity(tmp, cacheTCP);
                    }
                    else
                    {
                        tmp = new byte[TcpPackSize];
                        entity = new CacheEntity(tmp, null);
                    }
                    Array.Copy(BitConverter.GetBytes(data.Length), 0, tmp, 0, 4);
                    Array.Copy(data, 0, tmp, 4, TcpPackSize - 4);
                    index += TcpPackSize - 4;

                    return entity;
                }
            }
            else if (index + TcpPackSize < data.Length)
            {
                byte[] tmp = null;
                if (cacheTCP.GetBuffer(out tmp))
                {
                    entity = new CacheEntity(tmp, cacheTCP);
                }
                else
                {
                    tmp = new byte[TcpPackSize];
                    entity = new CacheEntity(tmp, null);
                }
                Array.Copy(data, tmp, TcpPackSize);
                index += TcpPackSize;

                return entity;
                
            }
            else
            {
                if(data.Length-index<=0)
                { return null; }
                byte[] tmp = new byte[data.Length - index];
                Array.Copy(data, tmp, tmp.Length);
                index += TcpPackSize;
                entity = new CacheEntity(tmp, null);
                return entity;
            }
        }

        /// <summary>
        /// 直接创建byte[]分包
        /// </summary>
        /// <param name="token"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static byte[] PackUDP(AsyncUdpUserToken token,ref int index)
        {
            if(token.Length==0)
            {
                token.Length = token.Data.Length;
            }
            //
            byte[] buf = null;
            if(index+UdpPackSize<=token.Length)
            {
                //分包
                buf = new byte[UdpPackSize];
                Array.Copy(token.Data, token.Offset + index, buf, 0, UdpPackSize);
                index += UdpPackSize;
            }
            else
            {
                int len = token.Length - index;
                buf = new byte[len];
                Array.Copy(token.Data, token.Offset + index, buf, 0, len);
                index += len;
            }
            return buf;
        }

        /// <summary>
        /// 缓存分包
        /// </summary>
        /// <param name="token"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static AsyncUdpUserToken PackCacheUDP(AsyncUdpUserToken token, ref int index)
        {
            if(cacheUDP==null)
            {
                cacheUDP = new CacheManager();
                cacheUDP.BufferSize = UdpPackSize;
                cacheUDP.MaxBufferCount = MaxUseBytes / UdpPackSize*2;
            }
            if(tokenPool==null)
            {
                tokenPool = new UserTokenPool();
               
            }
            if (token.Length == 0)
            {
                token.Length = token.Data.Length;
                
            }
            //
            byte[] buf = null;
            AsyncUdpUserToken userToken = tokenPool.Pop();
            userToken.Remote = token.Remote;
            userToken.IPAddress = token.IPAddress;
            userToken.IsFixCache = false;
            userToken.Socket = token.Socket;
            if (index + UdpPackSize <= token.Length)
            {
                //分包
                if (!cacheUDP.GetBuffer(out buf))
                {
                    buf = new byte[UdpPackSize];//与
                }
                Array.Copy(token.Data, token.Offset + index, buf, 0, UdpPackSize);
                index += UdpPackSize;
                userToken.Cache = cacheUDP;
                userToken.TokenPool = tokenPool;
            }
            else
            {
                int len = token.Length - index;
                buf = new byte[len];
                Array.Copy(token.Data, token.Offset + index, buf, 0, len);
                index += len;
                userToken.TokenPool = tokenPool;
            }
            return userToken;
        }

        /// <summary>
        /// 缓存分包；如果不到缓存长度就创建byte[],通过isUse
        /// </summary>
        /// <param name="token"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static AsyncUdpUserToken PackCacheUDPHead(AsyncUdpUserToken token, ref int index)
        {
            if (cacheUDP == null)
            {
                cacheUDP = new CacheManager();
                cacheUDP.BufferSize = UdpPackSize;
                cacheUDP.MaxBufferCount = MaxUseBytes / UdpPackSize * 2;
            }
            if (tokenPool == null)
            {
                tokenPool = new UserTokenPool();

            }
            if (token.Length == 0)
            {
                token.Length = token.Data.Length;

            }
            //
            byte[] buf = null;
            AsyncUdpUserToken userToken = tokenPool.Pop();
            userToken.Remote = token.Remote;
            userToken.IPAddress = token.IPAddress;
            userToken.IsFixCache = false;
            userToken.Socket = token.Socket;
            if(token.DataPackage==null)
            {
                token.DataPackage = new UDPDataPackage();
                token.DataPackage.packageID = Interlocked.Increment(ref dataPackageid);
                token.DataPackage.packageSeq = -1;
                token.DataPackage.packageSum = token.Length;
                token.DataPackage.data = token.Data;
                token.DataPackage.DataLen = token.Length;
                token.DataPackage.Offset = token.Offset;
                token.PackageNum = token.Length / UdpPackSize + 1;
                token.DataPackage.PackageNum = token.PackageNum;
            }
            if (index + UdpPackSize <= token.Length)
            {
                //分包
                if (!cacheUDP.GetBuffer(out buf))
                {
                    buf = new byte[UdpPackSize];//与
                }
                token.Offset = token.Offset + index;//移动偏移量
                token.DataPackage.Pack(buf, 0, UdpPackSize);
                userToken.Data = token.DataPackage.data;
                userToken.Length = UdpPackSize;
                // Array.Copy(token.Data, token.Offset + index, buf, 0, UdpPackSize);
                index += UdpPackSize;
                userToken.Cache = cacheUDP;
                userToken.TokenPool = tokenPool;
            }
            else
            {
                int len = token.Length - index;
                buf = new byte[len+ UDPDataPackage.HeadLen];//头
                token.Offset = token.Offset + index; //移动偏移量
                token.DataPackage.Pack(buf, 0, buf.Length);//这样做恰好合适，内部分包不判断
                //Array.Copy(token.Data, token.Offset + index, buf, 0, len);
                userToken.Data = buf;
                userToken.Length = buf.Length;
                index += len;
                userToken.TokenPool = tokenPool;
            }
            return userToken;
        }


    }
}
