using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;

namespace NettyTransmission
{


    /// <summary>
    /// 服务端适配器
    /// </summary>
    public class DataSrvAdapter
    {
        private static Lazy<DataSrvAdapter> instance = new Lazy<DataSrvAdapter>();

        public static List<NettyAddress> Addresses { get; set; }

        public static DataSrvAdapter Singleton
        {
            get { return instance.Value; }
        }

        private SemaphoreSlim semaphore = null;
        
        /// <summary>
        /// 接收数据
        /// </summary>
        private ConcurrentQueue<SrvDataSource> queue = null;

        private BlockingCollection<SrvDataSource> block = null;

        private int ReqCount = 0;//返回的数据量

         
        /// <summary>
        /// 每个处理线程处理的数据量
        /// 超过该数据量就扩展线程
        /// </summary>
        private const int ControlThreadNum = 10000;

        /// <summary>
        /// 是否正在运行调度线程
        /// </summary>
        private volatile bool IsControlRun = true;

        /// <summary>
        /// 30分钟毫秒数
        /// </summary>
        private const int MaxControlTime =30 * 60 * 1000;

        /// <summary>
        /// 数据回调
        /// </summary>
        public event NettyDataNotify SrvDataNotify;

        public DataSrvAdapter()
        {

            int num = Environment.ProcessorCount * 2;
            queue = new ConcurrentQueue<SrvDataSource>();
            block = new BlockingCollection<SrvDataSource>(num* ControlThreadNum);
            semaphore = new SemaphoreSlim(num, num);
            NettySrvManager.SrvAddress = Addresses;
            NettySrvManager.Singleton.Init();
            NettySrvManager.Singleton.DataNotify += Singleton_DataNotify;
            AdapterThread();
        }

        private void Singleton_DataNotify(object sender, object msg,string flage=null)
        {
            //事件收回数据，不影响网络传输
            SrvDataSource source = new SrvDataSource()
            {
                Context = sender,
                Message = msg,
                Flage=flage
            };
            Console.WriteLine("DataSrvAdapter数据注入集合");
            queue.Enqueue(source);
            Interlocked.Increment(ref ReqCount);
            if (!IsControlRun)
            {
                Console.WriteLine("DataSrvAdapter调度线程运行中");
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
        /// 开启处理线程
        /// </summary>
        private void Control()
        {
            int waitTime = MaxControlTime;
            int freeTime = MaxControlTime;
           
            while (true)
            {
                Console.WriteLine("Adapter调度线程运行中");
                //已经使用的线程
                int num = Environment.ProcessorCount*2 - semaphore.CurrentCount;
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
            Console.WriteLine("Adapter调度线程退出");
        }

        /// <summary>
        /// 分配返回数据
        /// </summary>
        private void Process()
        {
            //取出请求ID
            Console.WriteLine("DataSrvAdapter处理线程运行中");
            SrvDataSource source = null;
            while (!queue.IsEmpty)
            {
                Console.WriteLine("DataSrvAdapter接收数处理中");
                if (queue.TryDequeue(out source))
                {

                    var buf = source.Message as IByteBuffer;
                    if(buf!=null)
                    {
                        source.ID = buf.ReadLongLE();
                        byte[] rev = new byte[buf.ReadableBytes];
                        buf.ReadBytes(rev);
                        source.Message = rev;
                        Console.WriteLine("DataSrvAdapter接收数据：" + (rev.Length + 8));
                        Console.WriteLine("DataSrvAdapter接收数据通信ID：" + source.ID);
                        if (SrvDataNotify != null)
                        {
                            SrvDataNotify(this, source);
                        }
                        else
                        {
                            block.Add(source);
                        }
                        buf.Release();
                    }
                    
                }
           
            }
        }

        public SrvDataSource GetSrvData()
        {
            return block.Take();
        }
     
    }

   
}
