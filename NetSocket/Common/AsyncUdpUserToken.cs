/**
* 命名空间: NetSocket 
* 类 名：Class1 
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
    /// 功能描述    ：Class1 
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
  public  class AsyncUdpUserToken:AsyncUserToken
    {

        private CacheManager cache = null;
        private UserTokenPool tokenPool = null;

        /// <summary>
        /// 数据区偏移
        /// </summary>
        public int Offset { get; set; }

       /// <summary>
       /// 数据长度
       /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 暂时无用
        /// </summary>
        public bool IsUse { get; set; }

        /// <summary>
        /// 转为数据协议分包准备的
        /// </summary>
        public UDPDataPackage DataPackage { get; set; }

        /// <summary>
        /// 如果是缓存，是否是缓存区
        /// 标记不是本缓存区而是socket的缓存
        /// </summary>
        public bool IsCache { get; set; }

        /// <summary>
        /// 为协议分包准备，保持所有分包
        /// </summary>
        public List<AsyncUdpUserToken> ListPack { get; set; }

        /// <summary>
        /// 协议分包时小包个数
        /// </summary>
        public int PackageNum { get; set; }

        /// <summary>
        /// 数据区缓存
        /// </summary>
        public CacheManager Cache { set { cache = value; } }

        /// <summary>
        /// 接收实体缓存
        /// </summary>
        public UserTokenPool TokenPool { set { tokenPool = value; } }

        public AsyncUdpUserToken(CacheManager cache=null, UserTokenPool pool=null)
        {
            this.cache = cache;
            this.tokenPool = pool;
            this.CreateTime = DateTime.Now;
            this.DataTime = DateTime.Now;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void FreeCache()
        {
            if(cache!=null)
            {
                if (IsFixCache)
                {
                    cache.FreeBuffer(Offset);
                }
                else
                {
                    cache.FreePoolBuffer(Data);
                }
            }
            if(tokenPool!=null)
            {
                tokenPool.Push(this);
            }
        }
    }
}
