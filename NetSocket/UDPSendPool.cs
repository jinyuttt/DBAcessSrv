using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace NetSocket
{
   public class UDPSendPool
    {
        ConcurrentStack<AsyncUDPSendBuffer> stack = null;
        private const int time = 30;
        private DateTime lastTime = DateTime.Now;//上传监测空余
        private int removeNum = 0;//需要移除的
        private long tokenid = 0;//实体标识
        private DateTime emptyTime = DateTime.Now;//上传空余时间
        private const int emptyNum = 6;//清空
                                       /// <summary>
                                       /// 监测空闲的实体数
                                       /// 超过该值就移除
                                       ///CPU*10
                                       /// </summary>
        public int MaxLeftNum { get; set; }


        public UDPSendPool()
        {
            stack = new ConcurrentStack<AsyncUDPSendBuffer>();
            MaxLeftNum = Environment.ProcessorCount * 10;
        }

        /// <summary>
        /// 取出实体
        /// </summary>
        /// <returns></returns>
        public AsyncUDPSendBuffer Pop()
        {
            AsyncUDPSendBuffer buffer = null;
            if (!stack.TryPop(out buffer))
            {
                buffer = new AsyncUDPSendBuffer();
                buffer.ID = Interlocked.Increment(ref tokenid);
                buffer.Pool = this;
                emptyTime = DateTime.Now;
            }
            removeNum--;
            return buffer;
        }

        /// <summary>
        /// 置回实体
        /// </summary>
        /// <param name="token"></param>
        public void Push(AsyncUDPSendBuffer token)
        {

            if (removeNum > 0)
            {

                removeNum--;
                return;

            }
            stack.Push(token);
            Free();
        }

        /// <summary>
        /// 监测数据量
        /// </summary>
        private void Free()
        {
            if (removeNum > 0)
            {
                return;
            }
            if ((DateTime.Now - lastTime).TotalMinutes > time)
            {
                //监测最大保留
                removeNum = stack.Count - MaxLeftNum;
                lastTime = DateTime.Now;
                if ((DateTime.Now - emptyTime).TotalMinutes > time * emptyNum)
                {
                    //有浪费，只保留线程数据量
                    int num = Environment.ProcessorCount;
                    removeNum = stack.Count - num;
                }
            }
        }

        public void Clear()
        {
            stack.Clear();
        }
    }
}
