using System;
using System.Collections.Concurrent;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using DotNetty.Buffers;
using System.Collections.Generic;

namespace NettyTransmission
{

    /// <summary>
    /// 客户端适配器
    /// </summary>
    public class DataClientAdapter
    {
        private static Lazy<DataClientAdapter> instance = new Lazy<DataClientAdapter>();

        public static DataClientAdapter Singleton
        {
            get { return instance.Value; }
        }

        private static long  RequestID= 0;//分配网络ID

        /// <summary>
        /// 静态方法
        /// 处理地址，连接管理在构造方法中处理了连接
        /// </summary>
        /// <param name="list"></param>
        public static void AddSeverAddress(List<NettyAddress> list)
        {
            var dic = new Dictionary<string, List<NettyAddress>>();
            List<NettyAddress> tmp = null;
            foreach (var item in list)
            {
                if(string.IsNullOrEmpty(item.Flage))
                {
                    item.Flage = defaultSrv;
                }
                if(dic.TryGetValue(item.Flage,out tmp))
                {
                    if(!tmp.Contains(item))
                    {
                        tmp.Add(item);
                    }
                }
                else
                {
                    tmp = new List<NettyAddress>();
                    tmp.Add(item);
                    dic[item.Flage] = tmp;
                }
            }
            NettyClientManager.NettyAddresses = dic;
        }


        private ConcurrentDictionary<long, RequestNet> dicReslut = null;
        private SemaphoreSlim semaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
        
        /// <summary>
        /// 接收数据
        /// </summary>
        private ConcurrentQueue<object> block = null;

        private int ReqCount = 0;//返回的数据量

         
        /// <summary>
        /// 每个处理线程处理的数据量
        /// </summary>
        private const int ControlThreadNum = 10000;

        /// <summary>
        /// 是否正在运行调度线程
        /// </summary>
        private volatile bool IsControlRun = true;

        /// <summary>
        /// 10分钟毫秒数
        /// </summary>
        private const int MaxControlTime = 10 * 60 * 1000;

        private const string defaultSrv = "nettySrv";


        public DataClientAdapter()
        {
            dicReslut = new ConcurrentDictionary<long, RequestNet>(Environment.ProcessorCount*2, 100);
            block = new ConcurrentQueue<object>();
           
            NettyClientManager.Singleton.DataNotify += Singleton_DataNotify;
            AdapterThread();
        }

        private void Singleton_DataNotify(object sender, object msg,string flage)
        {
            //事件收回数据，不影响网络传输
            block.Enqueue(msg);
            Interlocked.Increment(ref ReqCount);
            if (!IsControlRun)
            {
                AdapterThread();
            }


        }

        /// <summary>
        /// 开启调度线程
        /// </summary>
        private void AdapterThread()
        {
            Thread thread = new Thread(Control);
            thread.IsBackground = true;
            thread.Name = "AdapterThread";
            thread.Start();
           
        }

        /// <summary>
        /// 启动处理线程，读取数据
        /// </summary>
        private void Control()
        {
            int waitTime = MaxControlTime;
            int freeTime = MaxControlTime;
            while (true)
            {
                //已经使用的线程
                Console.WriteLine("客户端适配器调度线程运行中");
                int num = Environment.ProcessorCount - semaphore.CurrentCount;
                if (ReqCount / ControlThreadNum + 1 > num)//正在使用的没有需求的多
                {
                    Task.Factory.StartNew(() =>
                    {
                        semaphore.Wait();
                        Process();
                        semaphore.Release();
                    });

                    /*
                  * 每隔1秒检查，相当于每秒才增加一个线程占用
                  * 给线程1秒的处理时间，平衡处理速度
                  * */
                    Thread.Sleep(1000);
                    freeTime = freeTime - 1000;
                    waitTime = MaxControlTime;
                }
                else
                {
                    //没有必要开启，需要更长休眠
                    Thread.Sleep(3000);
                    freeTime = freeTime - 3000;
                    waitTime = waitTime - 3000;
                }
                //10分钟没有参与调度则停止线程
                if(waitTime<=0)
                {
                    IsControlRun = false;
                    break;
                }

                //线程运行10分钟后，释放资源重启线程
                 if(freeTime<=0)
                {
                    AdapterThread();
                    break;
                }
            }
            Console.WriteLine("客户端适配器调度线程退出");
        }

        /// <summary>
        /// 分配返回数据
        /// </summary>
        private void Process()
        {

            //取出请求ID
            byte[] reqID = new byte[8];
            while (!block.IsEmpty)
            {
                Console.WriteLine("客户端适配器数据处理线程运行中");
                object rsp;
                if (block.TryDequeue(out rsp))
                {
                    Interlocked.Decrement(ref ReqCount);
                    var buf = rsp as IByteBuffer;
                    if(buf!=null)
                    {
                        long id = buf.ReadLong();
                        RequestNet request = null;
                        if (dicReslut.TryRemove(id, out request))
                        {
                            byte[] data = new byte[buf.ReadableBytes];
                            buf.ReadBytes(data);
                            request.Result = data;
                            request.ResetEvent.Set();
                        }
                        buf.Release();
                    }
               
                    
                }

            }
            Console.WriteLine("客户端适配器数据处理线程退出");
        }

        /// <summary>
        /// 投递数据
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        private RequestNet WaitSend(string clientFlage,byte[]req)
        {
            byte[] buf = new byte[req.Length + 8];
            RequestNet request = new RequestNet();
            request.ID = Interlocked.Increment(ref RequestID);
            using (var mem = new MemoryStream(buf))
            {
                mem.Write(BitConverter.GetBytes(request.ID), 0, 8);
                mem.Write(req, 0, req.Length);
            }
            req = null;//及时释放
            dicReslut[request.ID] = request;
            NettyClientManager.Singleton.Submit(clientFlage,buf);
            return request;
        }

        /// <summary>
        /// 只传输，无返回
        /// </summary>
        /// <param name="req"></param>
        public void Push(byte[] req,string clientFlage=defaultSrv)
        {
            var request = WaitSend(clientFlage,req);
            dicReslut.TryRemove(request.ID, out request);
        }
   
        /// <summary>
        /// 有返回的传输
        /// </summary>
        /// <param name="req">传输数据</param>
        /// <param name="timeOut">超时设置，默认一直等待</param>
        /// <returns></returns>
        public byte[] Request(byte[] req, string clientFlage = defaultSrv,int timeOut=0)
        {
            var request=   WaitSend(clientFlage,req);
            if (timeOut > 0)
            {
                request.ResetEvent.WaitOne(timeOut);
            }
            else
            {
                request.ResetEvent.WaitOne();
            }
            return request.Result;
        }

        /// <summary>
        /// 有返回的传输
        /// </summary>
        /// <param name="req">传输数据</param>
        /// <param name="result">返回结果</param>
        /// <param name="timeOut">超时设置，默认一直等待</param>
        /// <returns>是否正常返回</returns>
        public bool TryRequest(byte[] req,out byte[] result, string clientFlage = defaultSrv, int timeOut = 0)
        {

            var request = WaitSend(clientFlage,req);
            bool isSucess = false;
            if (timeOut > 0)
            {
                isSucess= request.ResetEvent.WaitOne(timeOut);
            }
            else
            {
                isSucess= request.ResetEvent.WaitOne();
            }
            result = request.Result;
            return isSucess;
        }

    }

   /// <summary>
   /// 请求模型
   /// </summary>
    internal class  RequestNet
    {
        public RequestNet()
        {
            ResetEvent = new AutoResetEvent(false);
        }
        public long ID { get; set; }

        public byte[] Result { get; set; }

        public AutoResetEvent ResetEvent { get; set; }
    }
}
