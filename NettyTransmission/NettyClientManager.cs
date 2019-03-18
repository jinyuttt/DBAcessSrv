using DotNetty.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NettyTransmission
{

    /// <summary>
    /// 该层只负责把数据搬走
    /// 使用该类前，需要处理静态属性NettyAddresses  
    /// 构造方法中已经初始化了地址连接
    /// </summary>
    public class NettyClientManager
    {
        private static Lazy<NettyClientManager> instance = new Lazy<NettyClientManager>();

        /// <summary>
        /// 客户端最大连接
        /// </summary>
        public static int MaxClientNum = Environment.ProcessorCount;

        /// <summary>
        /// 所有连接地址
        /// </summary>
        public static Dictionary<string,List<NettyAddress>> NettyAddresses { get; set; }

        public static NettyClientManager Singleton
        {
            get { return instance.Value; }
        }

        private const int SleepTime = 100;//毫秒
        private const int SleepNum = 1000;//1000包休眠
        private const int defaultTimeOut = 5000;//毫秒
        private const int defaultCacheNum = 1000;//缓存包数

        #region 客户端状态

        private const int Init = 0;
        private const int Close = 3;
        private const int Use =1;
        private const int Free = 2;
        private const int Open = 4;

        #endregion

      

        /// <summary>
        /// 每个线程需要处理的数据包
        /// 超过该值则增加一个处理线程
        /// </summary>
        private const int MaxThreadNum = 100;

        /// <summary>
        /// 待发送数据
        /// </summary>
        private readonly ConcurrentQueue<ClientData> queue = null;

        /// <summary>
        /// 接收数据
        /// </summary>
        private BlockingCollection<byte[]> revBlock = null;

        /// <summary>
        /// 异步提交无返回
        /// </summary>
        private readonly ConcurrentQueue<ClientData> callQueue = null;

        /// <summary>
        /// 客户端
        /// </summary>
        private readonly Dictionary<string, ClientConnect[]> dicClienStates = null;

  

        /// <summary>
        /// 控制处理线程最大数量
        /// </summary>
        private readonly SemaphoreSlim slim = null;

        private volatile bool isPushRun = false;//无返回提交
        private volatile bool isControlRun = false;//控制线程调度线程运行
        private  int WaitNum = 0;//所有等待的数据量
     

        /// <summary>
        /// 不能连接的服务端
        /// </summary>
        private ConcurrentDictionary<string, string> dicUnCon = null;

        /// <summary>
        /// 数据回传事件
        /// </summary>
        public event NettyDataNotify DataNotify;

        /// <summary>
        /// 构造方法
        /// </summary>
        public NettyClientManager()
        {
            revBlock = new BlockingCollection<byte[]>(1000);
            queue = new ConcurrentQueue<ClientData>();
            callQueue = new ConcurrentQueue<ClientData>();
            dicClienStates = new Dictionary<string, ClientConnect[]>();
            slim = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);
            dicUnCon = new ConcurrentDictionary<string, string>();
            if(ClientSettings.MemoryCacheNum<=0)
            {
                ClientSettings.MemoryCacheNum = defaultCacheNum;
            }
            InitClient();
            CheckConnect();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void InitClient()
        {
            foreach (var item in NettyAddresses)
            {
                ClientConnect[] clientStates = new ClientConnect[item.Value.Count];
                for (int i = 0; i < item.Value.Count; i++)
                {
                    var address = item.Value[i % item.Value.Count];
                    ClientConnect connect = new ClientConnect(this)
                    {
                        //State = Init,
                        Address = address,
                        //LastTick = DateTime.Now.Ticks,
                        ClientFlage = item.Key

                    };
                    connect.DataNotify += Connect_DataNotify;
                    clientStates[i] = connect;
                    connect.InitClient();
                }
                dicClienStates[item.Key] = clientStates;
            }
        }

       

        /// <summary>
        /// 一个线程提交数据到网络
        /// </summary>
        private void PushQueue()
        {
            isPushRun = true;
            Task.Factory.StartNew(() =>
            {
                ClientData data;
                int num = 0;
                while(!callQueue.IsEmpty)
                {
                    num++;
                    if(callQueue.TryDequeue(out data))
                    {
                        var client=  FindBest(data.ClientFlage);
                        Send(client.Result, data.Data,1);
                    }
                    if(num%SleepNum==0)
                    {
                        Thread.Sleep(SleepTime);
                    }
                }
                isPushRun = false;
            });
        }

        /// <summary>
        /// 每100个数据增加一个线程提交
        /// </summary>
        private void SubmitQueue()
        {
            Task.Factory.StartNew(() =>
            {
                slim.Wait();
                ClientData data;
                int num = 0;
                while (!queue.IsEmpty)
                {
                    num++;
                    if (queue.TryDequeue(out data))
                    {
                        num++;
                        Interlocked.Decrement(ref WaitNum);
                        Logger.Singleton.Debug("数据队列正在处理");
                        try
                        {
                            var client = FindBest(data.ClientFlage);
                            if (client != null && client.Result != null)
                            {
                                Send(client.Result, data.Data);
                            }
                            else
                            {
                                if(dicUnCon.ContainsKey(data.ClientFlage))
                                {
                                    //该类数据无法提交网络了
                                    if(WaitNum>ClientSettings.MemoryCacheNum)
                                    {
                                        Save(data);
                                        continue;
                                    }
                                }
                                SubmitAsync(data.ClientFlage, data.Data);
                            }
                        }
                        catch
                        {
                            SubmitAsync(data.ClientFlage, data.Data);
                        }
                    }
                    if (num % SleepNum == 0)
                    {
                        Thread.Sleep(SleepTime);
                    }
                }
                slim.Release();
            });
        }

       /// <summary>
       /// 启动控制线程
       /// </summary>
        private void ControlThread()
        {
            isControlRun = true;
            Thread thread = new Thread(AddThread);
            thread.IsBackground = true;
            thread.Name = "ClientNetControl";
            thread.Start();
           
        }

        /// <summary>
        /// 扩展数据处理线程
        /// </summary>
        private void AddThread()
        {
            int waitTime = 5 * 60 * 1000;
            int curTime = waitTime;
            while (true)
            {
                int num = WaitNum / MaxThreadNum + 1;//需要的线程
                int runNum = Environment.ProcessorCount - slim.CurrentCount;//正在运行的
                if (num > runNum)
                {
                    SubmitQueue();
                    Thread.Sleep(1000);
                    curTime = waitTime;
                }
                else
                {
                    Thread.Sleep(3000);
                    curTime -= 3000;
                }
                if(curTime<=0)
                {
                    //5分钟没有参与调度就退出
                    isControlRun = false;
                    break;
                }
            }
            isControlRun = false;
        }

        /// <summary>
        /// 数据提交网络
        /// </summary>
        /// <param name="data"></param>
        public void Submit(string clientFlage,byte[]data)
        {
            var client= FindFree(clientFlage);
            if (client != null)
            {
                Send(client, data);
            }
            else
            {
                //没有找到就提交到队列
                SubmitAsync(clientFlage,data);
            }
        }

        
        /// <summary>
        /// 可以异步提交
        /// </summary>
        /// <param name="data"></param>
        public  void SubmitAsync(string clientFlage,byte[]data)
        {
            Console.WriteLine("添加queue");
            ClientData clientData;
            clientData.ClientFlage = clientFlage;
            clientData.Data = data;
            queue.Enqueue(clientData);
            Interlocked.Increment(ref WaitNum);
            if(!isControlRun)//只要有数据提交就必须有控制线程
            {
                ControlThread();
            }
        }

        /// <summary>
        /// 只提交网络没有返回值
        /// </summary>
        /// <param name="data"></param>
        public void Push(string clientFlage, byte[]data)
        {
            ClientData clientData;
            clientData.ClientFlage = clientFlage;
            clientData.Data = data;
            callQueue.Enqueue(clientData);
            if(!isPushRun)
            {
                PushQueue();
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="client"></param>
        /// <param name="data"></param>
        private void Send(NettyClientState client,byte[]data,int commit=0)
        {
            if (client!=null&&client.IsConnected)
            {
                client.Client.Send(data);
                client.LastTick = DateTime.Now.Ticks;
                Interlocked.Increment(ref client.SubmitCount);
                Console.WriteLine("发送数据:"+data.Length);
            }
            else
            {
                if (commit == 0)
                {
                    SubmitAsync(client.ClientFlage, data);
                }
                else if(commit==1)
                {
                    Push(client.ClientFlage, data);
                }
            }
           
        }

        /// <summary>
        /// 找到一个空闲的
        /// </summary>
        /// <returns></returns>
        private NettyClientState FindFree(string clientFlage)
        {
           var client= dicClienStates[clientFlage];
            int len = client.Length;
            for (int i = 0; i < len; i++)
            {
                var c = client[i].FindFree();
                 if(c!=null)
                {
                    return c;
                }
               
            }
            return null;
        }

        /// <summary>
        /// 找到一个合适的连接客户端
        /// </summary>
        /// <returns></returns>
        private async Task<NettyClientState>  FindBest(string clientFlage)
        {

            var client = dicClienStates[clientFlage];
            int len = client.Length;
            List<NettyClientState> lst = new List<NettyClientState>();
            for (int i = 0; i < len; i++)
            {
                var c =await client[i].FindBestAsync();
                if (c != null)
                {
                    lst.Add(c);
                }
            }
            if (lst.Count == 0)
            {
                return null;
            }
            else if (lst.Count == 1)
            {
                return lst[0];
            }
            else
            {
                lst.Sort((x, y) => { return x.CommitRate.CompareTo(y.CommitRate); });
                return lst[0];
            }
        }

        private void Connect_DataNotify(object sender, object msg,string flage=null)
        {
          
            if (DataNotify != null)
            {
                DataNotify(this, msg);
            }
            else
            {
                revBlock.Add(msg as byte[]);
            }
           

        }

        /// <summary>
        /// 数据同步获取
        /// </summary>
        /// <returns></returns>
        public byte[] GetData()
        {
            return revBlock.Take();
        }

        /// <summary>
        /// 检查每个服务端连接情况
        /// </summary>
        /// <returns></returns>
        private  void CheckConnect()
        {
            Task.Factory.StartNew(async () =>
            {
                int timeOut = ClientSettings.TimeOut > 0 ? ClientSettings.TimeOut : defaultTimeOut;
                int Cur = timeOut;
                while (true)
                {
                    Logger.Singleton.Debug("服务端连接检查运行中");
                    Thread.Sleep(Cur);
                    int sum = 0;
                    foreach (var item in NettyAddresses)
                    {
                        int num = 0;
                        for (int i = 0; i < item.Value.Count; i++)
                        {
                            NettyClient client = new NettyClient();
                            await client.Start(item.Value[i].Host, item.Value[i].Port);
                            if (client.IsUnConnect)
                            {
                                num++;
                               
                            }
                            await client.Close();
                        }
                        if(num==item.Value.Count)
                        {
                            //所有服务端失效
                            dicUnCon[item.Key] = null;
                        }
                        else
                        {
                            //有网络恢复了
                            string v = null;
                            if(dicUnCon.TryRemove(item.Key,out v))
                            {
                                Read(item.Key);//读取数据，查看有没有需要传输的。
                            }
                        }
                        sum = sum + num;
                    }
                    //检查各个连接情况
                    foreach(var kv in dicClienStates)
                    {
                        for(int i=0;i<kv.Value.Length;i++)
                        {
                           await kv.Value[i].CheckFree();
                        }
                    }
                    if(sum==0)
                    {
                        //服务端没有异常
                        Cur += timeOut;
                        if(Cur>5*timeOut)
                        {
                            Cur = timeOut;
                        }
                    }
                    else
                    {
                        Cur = timeOut;
                    }
                }
            });
        }

        /// <summary>
        /// 同步写入
        /// </summary>
        /// <param name="clientData"></param>
        private void Save(ClientData clientData)
        {
            if(ClientSettings.IsSave)
            {
                ClientCacheFile.Write(clientData);
            }
            this.Connect_DataNotify(this, clientData.Data, clientData.ClientFlage);
        }

       /// <summary>
       /// 读取所有的文件内容传输
       /// 原本设计是按照不同服务端不同存储
       /// 
       /// 
       /// </summary>
        private void Read(string clientFlage)
        {
            if (ClientSettings.AutoReSend)
            {
                Task.Factory.StartNew(() =>
                {
                    var lst = ClientCacheFile.Read(clientFlage);
                    foreach (var item in lst)
                    {
                        Push(item.ClientFlage, item.Data);
                    }
                });
            }
        }

    }

   
   
}
