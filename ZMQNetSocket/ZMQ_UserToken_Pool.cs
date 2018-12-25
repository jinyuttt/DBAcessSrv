#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：ZMQNetSocket
* 项目描述 ：
* 类 名 称 ： ZMQ_UserToken_Pool
* 类 描 述 ：
* 命名空间 ：ZMQNetSocket
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2018
* 更新时间 ：2018
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2018. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion




using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace ZMQNetSocket
{

    /* ============================================================================== 
  * 功能描述：ZMQ_UserToken_Pool 
  * 创 建 者：jinyu
  * 修 改 者：jinyu
  * 创建日期：2018 
  * 修改日期：2018
  * ==============================================================================*/

   internal class ZMQ_UserToken_Pool
    {
        private ConcurrentStack<TCPUserToken> stack = null;
        private const int WaitTime = 10*60*1000;//10分钟
        private DateTime lastTime = DateTime.Now;//最后一次为空
         public ZMQ_UserToken_Pool()
        {
            stack = new ConcurrentStack<TCPUserToken>();
            Check();
        }
        public void Push(TCPUserToken token)
        {
            Interlocked.Decrement( ref token.Use);
            stack.Push(token);
        }

        public bool Pop(out TCPUserToken token)
        {
            if (stack.TryPop(out token))
            {
                Interlocked.Increment(ref token.Use);
                token.AcessTime = DateTime.Now;
                return true;
            }
            lastTime = DateTime.Now;
            return false;
        }

        /// <summary>
        /// 监测无用的
        /// </summary>
        private void Check()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(WaitTime);
                if((DateTime.Now-lastTime).TotalMilliseconds> WaitTime)
                {
                    int count = stack.Count;
                    List<TCPUserToken> lst = new List<TCPUserToken>(count);
                    TCPUserToken token = null;
                    for(int i=0;i<count;i++)
                    {
                        if(stack.TryPop(out token))
                        {
                            if((DateTime.Now - token.AcessTime).TotalMilliseconds <WaitTime)
                            {
                                token.Socket.Close();
                                token.Socket.Dispose();
                            }
                            else
                            {
                                int num = stack.Count > lst.Count ? stack.Count - lst.Count : 0;
                                for (int j = 0; j < num; j++)
                                {
                                    if (stack.TryPop(out token))
                                    {
                                        token.Socket.Close();
                                        token.Socket.Dispose();
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                //
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                }
            });
        }
    }
}
