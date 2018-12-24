using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace NetSocket
{

    /// <summary>
    /// 同端口+IP数据
    /// </summary>
    internal class RecviceData
    {

        public event OnReceiveUdpData OnDataReceived;

        public event PushLossPackage OnLossData;

        public EndPoint remote;

        /// <summary>
        /// 接收队列
        /// </summary>
        private ConcurrentDictionary<long, RecvicePool> dicPool = null;

        /// <summary>
        /// 接收完成序列
        /// </summary>
        private ConcurrentDictionary<long, DateTime> dicSucess = null;

        private DateTime minTime = DateTime.Now;//完成的ID移除

        private const int MaxWaitSucess = 20;//维持接收完成的时间(秒)

        public RecviceData()
        {
            dicPool = new ConcurrentDictionary<long, RecvicePool>();
            dicSucess = new ConcurrentDictionary<long, DateTime>();
        }


        /// <summary>
        /// 保持数据
        /// </summary>
        /// <param name="package"></param>
        public void Add(UDPDataPackage package)
        {
            RecvicePool pool = null;
            //排除重复
            if (dicSucess.ContainsKey(package.packageID))
            {
                LosPackage los = new LosPackage();
                los.packageType = 3;
                los.packageID = package.packageID;
                SendBack(new LosPackage[] { los });
                return;//无用数据了；
            }
            if (dicPool.TryGetValue(package.packageID, out pool))
            {
                List<LosPackage> result=  pool.Add(package);
                if (result != null)
                {
                    Pool_OnLossData(this, remote, result.ToArray());
                    result.Clear();
                }
            }
            else
            {
                pool = new RecvicePool(package.PackageNum);
                pool.remote = remote;
                pool.OnLossData += Pool_OnLossData;
                pool.OnReviceData += Pool_OnReviceData;
                dicPool[package.packageID] = pool;
                List<LosPackage> result = pool.Add(package);
                if (result != null)
                {
                    Pool_OnLossData(this, remote, result.ToArray());
                    result.Clear();
                }

            }
            CheckSucess();
        }

        private void Pool_OnReviceData(object sender, long id, byte[] data, RecviceState state)
        {
            //一组接收完成
            if(dicSucess.ContainsKey(id))
            {
                return;
            }
            dicSucess[id] = DateTime.Now;
            RecvicePool pool = null;
            dicPool.TryRemove(id, out pool);
            if (OnDataReceived != null)
            {
                AsyncUdpUserToken token = new AsyncUdpUserToken();
                token.Data = data;
                token.Remote = remote;
                token.Length = data.Length;
                Task.Factory.StartNew(() =>
                {
                    OnDataReceived(this, token);
                });
                RecvicePool recvice = sender as RecvicePool;
                if (recvice != null)
                {
                    recvice.Clear();
                }
                recvice.OnLossData -= Pool_OnLossData;
                recvice.OnReviceData -= Pool_OnReviceData;
                   //完成发送一次
                    LosPackage package = new LosPackage();
                    package.packageType = 3;
                    package.packageID = id;
                    Pool_OnLossData(this, remote, new LosPackage[] { package});
                  
                    Console.WriteLine("sucess:" + id);
                
            }
        }

        private void Pool_OnLossData(object sender, object remote, LosPackage[] list)
        {
            if(OnLossData!=null)
            {
                Task.Factory.StartNew(() =>
                {
                    OnLossData(this, remote, list);
                });
            }
        }

        /// <summary>
        /// 移除到期成功ID
        /// </summary>
        private void CheckSucess()
        {
            if ((DateTime.Now - minTime).TotalSeconds < MaxWaitSucess)
            {
                return;
            }
            long id = -1;
            DateTime date;
            minTime = DateTime.Now;
            ConcurrentBag<long> bag = new ConcurrentBag<long>();
            Parallel.ForEach(dicSucess, (kv) =>
            {
                if ((DateTime.Now - kv.Value).TotalSeconds > MaxWaitSucess)
                {
                    bag.Add(kv.Key);
                    if (minTime > kv.Value)
                    {
                        minTime = kv.Value;
                    }
                }
            });
            do
            {
                if (bag.TryTake(out id))
                {
                    dicSucess.TryRemove(id, out date);
                }
            } while (!bag.IsEmpty);
        }

        /// <summary>
        /// 清理所有数据
        /// </summary>
        public void Clear()
        {
            long[] keys = new long[dicPool.Count];
            dicPool.Keys.CopyTo(keys, 0);
            RecvicePool pool = null;
            foreach (var key in keys)
            {
                if(dicPool.TryRemove(key, out pool))
                {
                    pool.Clear();
                }
                
            }
            dicPool.Clear();
        }

        /// <summary>
        /// 数据返回
        /// </summary>
        /// <param name="list"></param>
        private void SendBack(LosPackage[] list)
        {
            Task.Factory.StartNew(() =>
            {
               foreach(LosPackage los in list)
                {
                 
                    Pool_OnLossData(this, remote, list);
                }
            });
        }
    }
}
