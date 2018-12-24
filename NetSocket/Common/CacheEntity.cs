/**
* 命名空间: NetSocket 
* 类 名：CacheEntity 
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
    /// 功能描述    ：CacheEntity 发送数据缓存实体，专为客户端发送
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
   public class CacheEntity
    {
        private DateTime dateTime = DateTime.Now;
        private int offset;
        private byte[] data;
        private int length;
        private CacheManager buffer =null;
        private int fixSize = 0;
       
        public DateTime Time { get { return dateTime; } }

        public int Fix { get { return fixSize; } }

        public byte[] Buffer { get { return data; } }

        public int Offset { get { return offset; } }

        public int Length { get { return length; } }

        public CacheEntity(byte[]buf,int offset=0,int len=0, CacheManager buffer =null)
        {
            data = buf;
            this.offset = offset;
            this.length = len == 0 ? buf.Length : len;
            this.buffer = buffer;
        }
        public CacheEntity(byte[]buf,CacheManager cache=null)
        {
            data = buf;
            buffer = cache;
            fixSize = 1;
            this.length = buf.Length;
            this.offset = 0;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            if(buffer!=null)
            {
                if (Fix == 0)
                {
                    buffer.FreeBuffer(offset);
                }
                else
                {
                    buffer.FreePoolBuffer(data);
                }
            }
            this.buffer = null;
            this.data = null;
        }
    }
}
