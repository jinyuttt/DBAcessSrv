/**
* 命名空间: NetSocket 
* 类 名：BufferManager 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：BufferManager SocketAsyncEventArgs分配数据缓存
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
    public class BufferManager
    {
        public readonly static BufferManager instance = new BufferManager();
        private volatile bool isClose = false;

        #region 固定大小
        private volatile int m_numBytes = 1024;                 // 缓存池总大小
        private byte[] m_buffer;                // 缓存数据区 
        private ConcurrentStack<int> m_freeIndexPool;     //   释放回来的索引
        private volatile int m_currentIndex=0;//当前使用的全局索引
        private volatile int m_bufferSize = 1460;//每个buffer大小
        private object lock_obj = new object();//锁定对象
        #endregion

        #region 另外一种方式（动态增长），根据自己情况采用

        private ConcurrentStack<byte[]> m_cache = null;
        private int increasementNum = 16;//一般是8*2线程
        private volatile int hourMaxNum =0;
        private volatile int currentHour = 0;
        private volatile   int destroyNum = 0;//待销毁
        #endregion

        #region 订制大小方式
        /// <summary>
        /// 容量
        /// </summary>
        public int Capacity
        {
            get { return m_numBytes; }
            set { m_numBytes = value; }
         }

        /// <summary>
        /// 当前已经使用的量
        /// </summary>
        public int CurrentSize
        {
            get { return m_currentIndex; }
        }
        public BufferManager(int totalBytes, int bufferSize)
        {
            m_numBytes = totalBytes;
            m_currentIndex = 0;
            m_bufferSize = bufferSize;
            m_freeIndexPool = new ConcurrentStack<int>();
        }



        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if(isClose)
            {
                return false;
            }
            int index = -1;
            if (m_freeIndexPool.TryPop(out index))
            {
                args.SetBuffer(m_buffer, index, m_bufferSize);
            }
            else
            {
                lock (lock_obj)
                {
                    if (m_buffer == null)
                    {
                        m_buffer = new byte[m_numBytes];
                    }
                    if ((m_numBytes - m_bufferSize) < m_currentIndex)
                    {
                        //没有足够的缓存了
                        return false;
                    }
                    args.SetBuffer(m_buffer, m_currentIndex, m_bufferSize);
                    m_currentIndex += m_bufferSize;//递增索引
                }
            }
            return true;
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <param name="args"></param>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            if(isClose)
            {
                return;
            }
            m_freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
        #endregion

        #region 动态增长，该方式的优势是可以动态释放，劣势是需要动态增加
        /// <summary>
        /// 每次增长的缓存实体个数
        /// </summary>
        public int IncreasementNum
        {
            get { return increasementNum; }
            set { increasementNum = value; }
        }

        /// <summary>
        /// 每个缓存大小
        /// 默认1460字节
        /// </summary>
        public int BufferSize
        {
            get { return m_bufferSize; }
            set { m_bufferSize = value; }
        }

        /// <summary>
        /// 最大缓存个数
        /// 默认1024;
        /// </summary>
        public int MaxBufferCount
        {
            //共用参数，注意不同方式
            get { return m_numBytes; }
            set { m_numBytes = value; }
        }

        /// <summary>
        /// 剩余的缓存实体个数
        /// </summary>
        public int LeftBufferNum
        {
            get { return m_cache.Count; }
        }
        public BufferManager()
        {

            m_currentIndex = 0;
            m_cache = new ConcurrentStack<byte[]>();
            m_freeIndexPool = new ConcurrentStack<int>();//初始化

        }

        public bool GetBuffer(SocketAsyncEventArgs args)
        {
            if(isClose)
            {
                return false;
            }
            byte[] buf = null;
            if (m_cache.TryPop(out buf))
            {
                args.SetBuffer(buf, 0, m_bufferSize);
            }
            else
            {
                Increase();
                if (m_currentIndex < m_numBytes)
                {
                    m_currentIndex++;
                    buf = new byte[m_bufferSize];
                    args.SetBuffer(buf, 0, m_bufferSize);
                }

            }
            return true;
        }

        /// <summary>
        /// 缓存放回
        /// </summary>
        /// <param name="args"></param>
        public void FreePoolBuffer(SocketAsyncEventArgs args)
        {
            if(isClose)
            {
                return;
            }
            if(destroyNum<1)
            {
                m_cache.Push(args.Buffer);
            }
            else
            {
                destroyNum--;
            }
            args.SetBuffer(null, 0, 0);
            Refresh();

        }

        /// <summary>
        /// 增长
        /// </summary>
        private bool Increase()
        {
            if (m_cache.IsEmpty && m_currentIndex < m_numBytes)
            {
                for (int i = 0; i < increasementNum; i++)
                {
                    m_cache.Push(new byte[m_bufferSize]);
                }
                m_currentIndex += increasementNum;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 记录当前一小时最大使用
        /// 释放时记录，不影响使用
        /// </summary>
        private void Refresh()
        {
            if(currentHour!=DateTime.Now.Hour)
            {
                //时间更换
                if(LeftBufferNum>IncreasementNum)
                {
                    //剩余过大，根据当前一小时的最大值回收
                    destroyNum = m_currentIndex - hourMaxNum - increasementNum;
                }
                hourMaxNum = -1;
                currentHour = DateTime.Now.Hour;
            }
            if(hourMaxNum<m_currentIndex)
            {
                //记录当前需要使用的情况
                hourMaxNum = m_currentIndex;
            }
        }
        #endregion

        public void  Clear()
        {
            lock (lock_obj)
            {
                isClose = true;
                m_buffer = null;
                m_freeIndexPool.Clear();
                m_freeIndexPool = null;
                m_cache.Clear();
                m_cache = null;
            }

        }
    }
}
