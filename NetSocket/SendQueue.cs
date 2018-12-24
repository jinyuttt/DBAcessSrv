using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSocket
{
    public delegate void PushLossReset(object sender, AsyncUdpUserToken[] list);

   /// <summary>
   /// 发送队列
   /// </summary>
    public  class SendQueue
    {
        AsyncUdpUserToken AsyncUdp;
        AutoResetEvent resetEvent = null;
        public event PushLossReset PushLossReset;
        private const int MaxWaitTime = 10;
        private DateTime lastTime = DateTime.Now;
        public long packageID = 0;
        private bool isClear = false;//是否调用了清理

       /// <summary>
       /// 发送的根数据
       /// </summary>
       /// <param name="token"></param>
        public SendQueue(AsyncUdpUserToken token)
        {
            AsyncUdp = token;
            packageID = token.DataPackage.packageID;
            resetEvent = new AutoResetEvent(false);
            Check();//创建时说明数据已经发送了
        }


        /// <summary>
        /// 验证发送的数据接收情况
        /// </summary>
        private void Check()
        {
            Task.Factory.StartNew(() =>
            {
                resetEvent.WaitOne(100);//100ms验证一次
               
                List<AsyncUdpUserToken> list = new List<AsyncUdpUserToken>();
                if (AsyncUdp.ListPack.Count >0)
                {
                    foreach(AsyncUdpUserToken item in AsyncUdp.ListPack)
                    {
                        if(item!=null)
                        {
                            list.Add(item);
                        }
                    }
                }
                if(PushLossReset!=null&&list.Count>0)
                {
                    PushLossReset(this, list.ToArray());
                }
                if(list.Count>0&&(DateTime.Now-lastTime).TotalSeconds<MaxWaitTime&& !isClear)
                {
                    Check();
                }
            });
        }

        /// <summary>
        /// 接收完成
        /// </summary>
        /// <param name="seq"></param>
        public  void Add(int  seq)
        {
            lastTime = DateTime.Now;
            if (AsyncUdp.ListPack!=null)
            {
                if(AsyncUdp.ListPack.Count>seq)
                {
                    try
                    {
                        //ListPack没有同步
                        AsyncUdpUserToken current = AsyncUdp.ListPack[seq];
                        packageID = current.DataPackage.packageID;
                        current.FreeCache();
                        AsyncUdp.ListPack[seq] = null;
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// 接收完成
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public AsyncUdpUserToken GetAsyncUdpUserToken(int seq)
        {
            lastTime = DateTime.Now;
            if (AsyncUdp.ListPack != null)
            {
                if (AsyncUdp.ListPack.Count > seq)
                {
                    AsyncUdpUserToken current = AsyncUdp.ListPack[seq];
                    return current;
                }
            }
            return null;
        }

        /// <summary>
        /// 清除
        /// </summary>
        public void Clear()
        {
            AsyncUdp.FreeCache();
            AsyncUdp.ListPack.Clear();
            resetEvent.Set();
            isClear = true;
        }
    }
}
