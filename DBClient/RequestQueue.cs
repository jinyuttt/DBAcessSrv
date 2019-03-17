using DBModel;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Serializer;
using System;

namespace DBClient
{


    public delegate void ExecuteError(long id,bool isSucess, string msg);

    /// <summary>
    /// 执行队列
    /// </summary>
   public class RequestQueue
    {
        private ConcurrentQueue<DBTransfer> queue = null;
       
        private SemaphoreSlim semaphore = null;
        private int state = 0;


        public static readonly RequestQueue Instance = new RequestQueue();
        /// <summary>
        /// 执行结果返回
        /// </summary>
        public event ExecuteError SrvExecuteResult;

        public RequestQueue()
        {
            queue = new ConcurrentQueue<DBTransfer>();
            semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        }

        /// <summary>
        /// 放置操作
        /// </summary>
        /// <param name="transfer"></param>
        public void Push(DBTransfer transfer)
        {
            queue.Enqueue(transfer);
            if(Interlocked.CompareExchange(ref state,1,0)==0)
            {
                Start();
            }
        }

       /// <summary>
       /// 启动队列
       /// </summary>
        private void Start()
        {
            Send();
        }

        private void Send()
        {
            Task.Factory.StartNew(() =>
            {
                DBTransfer transfer = null;
                int num = 0;
                int waitTime = 300;//5分钟
                while (true)
                {
                    if (queue.IsEmpty)
                    {
                        Thread.Sleep(1000);
                        num++;
                        if(num>waitTime)
                        {
                            break;
                        }
                        continue;
                    }
                    num = 0;
                    if (queue.TryDequeue(out transfer))
                    {
                        semaphore.Wait();
                        Task.Factory.StartNew(() =>
                        {
                            byte[] buf = SerializerFactory<CommonSerializer>.Serializer(transfer);
                            RequestServer request = new RequestServer();
                            request.Address = SrvControl.Instance.GetCureent();
                            byte[] rec = request.Request(buf);
                            RequestResult result = SerializerFactory<CommonSerializer>.Deserialize<RequestResult>(rec);
                            if (SrvExecuteResult != null)
                            {
                                SrvExecuteResult(result.RequestID, result.Error == ErrorCode.Sucess, result.ReslutMsg);
                            }
                            semaphore.Release();
                        });
                       

                    }

                }
                Send();//递归更新线程
            });
        }

        
    }
}
