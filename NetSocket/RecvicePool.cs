using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSocket
{
   

    public enum RecviceState
    {
        sucess,
        fail
    }

    /// <summary>
    /// 一组数据
    /// </summary>
  public  class RecvicePool
    {
        public event RecvicePoolData OnReviceData;
        public event PushLossPackage OnLossData;

        public RecviceBuffer[] buf =null;
        public long id = 0;
        public long sum = 0;
        public EndPoint remote;
        private Dictionary<long, LosPackage> dicLosss = new Dictionary<long, LosPackage>();
        private int max =0;
        AutoResetEvent resetEvent = null;
        private  long  packageSum=0;
        byte[] currten = null;
        private const int MaxWaitTime = 10;
        private const int MaxBufferSize = 100*1024*1024;//100M;
        private const int MaxOneRecviceSize = 100 * 1024 * 1024;//1G

        public DateTime LastTime { get; set; }
        public RecvicePool(int num)
        {
            resetEvent = new AutoResetEvent(false);
            buf = new RecviceBuffer[num];
            Check();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public List<LosPackage> Add(UDPDataPackage package)
        {
            if(currten==null)
            {
                if (package.packageSum < MaxBufferSize)
                {
                    currten = new byte[package.packageSum];
                }
                else
                {
                    currten = new byte[MaxBufferSize];
                }
            }
            LastTime = DateTime.Now;
            if(buf==null)
            {
                return null;
            }
            if (buf[package.packageSeq] == null)
            {
                buf[package.packageSeq] = new RecviceBuffer() { data = package.data };
                sum += package.data.Length;
            }
            packageSum = package.packageSum;
            id = package.packageID;
            List<LosPackage> list = new List<LosPackage>();
            LosPackage rec = new LosPackage() { packageID = package.packageID, packageSeq = package.packageSeq, packageType = 1 };
            if (package.packageSeq != 0)
            {

                if (max < package.packageSeq - 1)
                {
                    for (int i = max + 1; i < package.packageSeq; i++)
                    {
                        LosPackage tmp = new LosPackage() { packageType = 2, packageID = package.packageID, packageSeq = i };
                        dicLosss[i] = tmp;
                        list.Add(tmp);
                    }
                    max = package.packageSeq;
                }
            }
            list.Add(rec);
            dicLosss.Remove(package.packageSeq);
            if(sum>=packageSum)
            {
                resetEvent.Set();
            }
            return list;
        }


        /// <summary>
        /// 验证接收情况
        /// </summary>
        private void Check()
        {

            Console.WriteLine("check");
            Task.Factory.StartNew(() =>
            {
                resetEvent.WaitOne();
                long cur = 0;
                bool sucess = false;
                for(int i=0;i<buf.Length;i++)
                {
                    if(buf[i]!=null)
                    {

                        Array.Copy(buf[i].data, 0, currten, cur, buf[i].data.Length);
                        cur += buf[i].data.Length;
                    }
                     if(cur==packageSum||cur>=MaxBufferSize)
                    {
                        //接收完成
                       // byte[] currten = new byte[packageSum];
                        sucess = true;
                        if (OnReviceData!=null)
                        {
                            OnReviceData(this,id, currten, RecviceState.sucess);
                        }
                        break;
                    }
                    else
                    {
                        LosPackage tmp = new LosPackage() { packageType = 2, packageID = id, packageSeq = i };
                        dicLosss[i] = tmp;
                        if((DateTime.Now-LastTime).TotalSeconds>MaxWaitTime)
                        {
                            if (OnReviceData != null)
                            {
                                OnReviceData(this, id, null, RecviceState.fail);
                            }
                            sucess = true;
                           
                        }
                        else
                        {
                            LosPackage[] lostTmp = new LosPackage[dicLosss.Count];
                            dicLosss.Values.CopyTo(lostTmp, 0);
                            OnLossData(this,remote, lostTmp);
                        }
                        break;
                    }
                }
                
                if (!sucess)
                {
                    Check();
                }
                
            });
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        public void Clear()
        {
            this.resetEvent.Set();
            this.buf = null;
            this.currten = null;
            this.dicLosss.Clear();
            GC.Collect();
        }

    }
}
