using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace NetSocket
{
    public delegate void RecvicePoolData(object sender, long id, byte[] data, RecviceState state);
    public delegate void PushLossPackage(object sender,object remote, LosPackage[] list);

    /// <summary>
    /// 接收处理
    /// </summary>
    internal class SocketEndPoint
    {
        private  ConcurrentDictionary<string, RecviceData> dicPool = null;//数据
        private ConcurrentDictionary<string, DateTime> dicRecvice = null;//最后接收数据的世界
        public event OnReceiveUdpData OnDataReceived;
        public event PushLossPackage OnLossData;
        private int maxEndPointTime = 10;//没有该IP+Port数据则移除信息的时间（分钟）
        private object lock_obj = new object();
        public SocketEndPoint()
        {
            dicPool = new ConcurrentDictionary<string, RecviceData>();
            dicRecvice = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="token"></param>
        public void Add(AsyncUdpUserToken token)
        {
            IPEndPoint endPoint = token.Remote as IPEndPoint;
            if (null != endPoint)
            {
                string key = endPoint.Address.ToString() + endPoint.Port;
                dicRecvice[key] = DateTime.Now;
                RecviceData data = null;
                UDPDataPackage package = new UDPDataPackage();
                package.UnPack(token.Data, token.Offset, token.Length);
                if (dicPool.TryGetValue(key, out data))
                {
                    data.Add(package);
                }
                else
                {
                    lock (lock_obj)
                    {
                        //阻塞创建
                        if (!dicPool.TryGetValue(key, out data))
                        {
                            data = new RecviceData();
                            dicPool[key] = data;
                            data.remote = endPoint;
                            data.OnDataReceived += Data_OnDataReceived;
                            data.OnLossData += Data_OnLossData;
                            data.Add(package);
                        }
                        else
                        {
                            data.Add(package);
                        }
                    }

                }
            }
        }


        /// <summary>
        /// 验证无效的信息
        /// </summary>
        public void Validate()
        {
               string[] keys = new string[dicRecvice.Count];
               dicRecvice.Keys.CopyTo(keys, 0);
               Parallel.ForEach(keys, key => {
                DateTime dateTime;
                RecviceData recviceData = null;
                if(dicRecvice.TryGetValue(key, out dateTime))
                {
                    if((DateTime.Now-dateTime).TotalMinutes>maxEndPointTime)
                    {
                        //移除
                        if(dicPool.TryRemove(key, out recviceData))
                        {
                               recviceData.Clear();
                               recviceData.OnDataReceived -= Data_OnDataReceived;
                               recviceData.OnLossData -= Data_OnLossData;
                        }
                        dicRecvice.TryRemove(key,out dateTime);
                    }
                }
            }
            );
        }


        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            string[] keys = new string[dicPool.Count];
            dicPool.Keys.CopyTo(keys, 0);
            Parallel.ForEach(keys, key => {
                RecviceData recviceData = null;
                if (dicPool.TryGetValue(key, out recviceData))
                {
                    //移除
                    if (dicPool.TryRemove(key, out recviceData))
                    {
                        recviceData.Clear();
                        recviceData.OnDataReceived -= Data_OnDataReceived;
                        recviceData.OnLossData -= Data_OnLossData;
                    }

                }
            }
         );
            keys = null;
            dicPool.Clear();
            dicRecvice.Clear();

        }
        private void Data_OnLossData(object sender,object remote, LosPackage[] list)
        {
           if(OnLossData!=null)
            {
                OnLossData(this,remote, list);
            }
        }

        private void Data_OnDataReceived(object sender, AsyncUdpUserToken token)
        {
           if(OnDataReceived!=null)
            {
                OnDataReceived(this, token);
            }
        }
    }
}

