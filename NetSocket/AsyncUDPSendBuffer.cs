using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace NetSocket
{
   public class AsyncUDPSendBuffer
    {

        /// <summary>
        /// 缓存区
        /// </summary>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// 偏移
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// 池源
        /// </summary>
        public UDPSendPool Pool { get; internal set; }

        /// <summary>
        /// 长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 远端地址
        /// </summary>
        public EndPoint EndPoint { get; set; }

        /// <summary>
        /// ID
        /// </summary>
        public long ID { get; internal set; }

        /// <summary>
        /// 数据缓存源
        /// </summary>
        public CacheManager BufferCache { get; set; }

        /// <summary>
        /// 缓存类型
        /// </summary>
        public bool IsFixCache { get; set; }

        /// <summary>
        /// 额外信息
        /// </summary>
        public object Token { get; set; }
        
        /// <summary>
        /// 释放数据缓存
        /// </summary>
        public void FreeDataCache()
        {
            if(BufferCache!=null)
            {
                if (IsFixCache)
                {
                    BufferCache.FreeBuffer(Offset);
                }
                else
                {
                    BufferCache.FreePoolBuffer(Buffer);
                }
            }
        }

        /// <summary>
        /// 释放本实体
        /// </summary>
        public void Free()
        {
            if(Pool!=null)
            {
                Pool.Push(this);
            }
        }
    }
}
