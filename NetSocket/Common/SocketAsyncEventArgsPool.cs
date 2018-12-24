/**
* 命名空间: NetSocket 
* 类 名：SocketAsyncEventArgsPool 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：SocketAsyncEventArgsPool SocketAsyncEventArgs缓存
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    internal class SocketAsyncEventArgsPool
    {
        private ConcurrentStack<SocketAsyncEventArgs> stack = null;
        private int maxLeftNum = 1000;
        private volatile bool isClose = false;

        /// <summary>
        /// 最大剩余量
        /// </summary>
        public int MaxLeftNum
        {
            get { return maxLeftNum; }
            set { maxLeftNum = value; }
        }
      
        public SocketAsyncEventArgsPool()
        {
            stack = new ConcurrentStack<SocketAsyncEventArgs>();
          
        }

        /// <summary>
        /// 取出缓存实体
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            if(isClose)
            {
                return null;
            }
            SocketAsyncEventArgs eventArgs = null;
            if(!stack.TryPop(out eventArgs))
            {
                 eventArgs = new SocketAsyncEventArgs();
                 return eventArgs;
            }
            return eventArgs;
        }

        /// <summary>
        /// 释放缓存实体
        /// </summary>
        /// <param name="eventArgs"></param>
        public void Push(SocketAsyncEventArgs eventArgs)
        {
            if(eventArgs==null)
            { return; }
            if(isClose)
            {
                eventArgs.SetBuffer(null, 0, 0);
                eventArgs.Dispose();
                return;
            }
            if (stack.Count < maxLeftNum)
            {
                stack.Push(eventArgs);
            }
            else
            {
                eventArgs.SetBuffer(null, 0, 0);
                eventArgs.Dispose();
            }
        }

        /// <summary>
        /// 缓存实体个数
        /// </summary>
        public int Count
        {
            get { return stack.Count; }
        }

        /// <summary>
        /// 清除所有缓存实体
        /// </summary>
        public void Clear()
        {
            while(!stack.IsEmpty)
            {
                SocketAsyncEventArgs args = null;
                if(stack.TryPop(out args))
                    {
                    args.SetBuffer(null, 0, 0);
                    args.Dispose();
                }
            }
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Dispose()
        {
            isClose = true;
            Clear();

        }
    }
}
