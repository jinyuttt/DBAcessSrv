#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：nexcs
* 项目描述 ：
* 类 名 称 ：DBQueryServer
* 类 描 述 ：
* 命名空间 ：nexcs
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


using DBModel;
using NetSocket;
using Serializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace nexcs
{
    /* ============================================================================== 
    * 功能描述：DBQueryServer 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

   public class DBQueryServer
    {
        UDPSession udp = null;
        DBServer.DBServer server = null;
        Semaphore semaphore = null;//最大执行的线程数
        ConcurrentDictionary<long, DateTime> dicExecute = null;//正在执行的线程
        ConcurrentDictionary<long, DateTime> dicTimeOut = null;//超时的任务
      
        private const int MaxThreadNum = 300;//最大执行线程
        private static long ResponeID = 0;
        private const int Timeout = 10000;//毫秒
        private int checkNum = 0;//超时计算
        private volatile bool isCheck = false;
        /// <summary>
        /// 任何任务的最大超时时间
        /// 如果不能确定，应该是底层自己控制，保障时间
        /// </summary>
        private const int MaxExecuteTime = 10 * 60;//10分钟，600秒

        public DBQueryServer()
        {
            server = new DBServer.DBServer();
            semaphore = new Semaphore(MaxThreadNum, MaxThreadNum);
            dicExecute = new ConcurrentDictionary<long, DateTime>();
            udp = new UDPSession();
            dicTimeOut = new ConcurrentDictionary<long, DateTime>();
        }
        public void Start(int port=7777,string host=null)
        {
            udp.Port = port;
            udp.Host = host;
            udp.Bind();
            udp.OnDataReceived += Udp_OnDataReceived;
        }

        private void Udp_OnDataReceived(object sender, AsyncUdpUserToken token)
        {
            byte[] buffer = null;
            if(token.Offset==0&&(token.Length==0||token.Data.Length==token.Length))
            {
                buffer = token.Data;
            }
            else
            {
                buffer = new byte[token.Length];
                Array.Copy(buffer, 0, token.Data, token.Offset, token.Length);
            }
            DBTransfer transfer= SerializerFactory<CommonSerializer>.Deserialize<DBTransfer>(buffer);
            token.FreeCache();
            Task.Factory.StartNew(() =>
            {
                ProcessMonitor(transfer, token.Remote);
            });
                
            //

        }

        /// <summary>
        /// 监视执行
        /// 说明：task.Wait(cts.Token)会异常，同时注册的事件无效；
        /// 采用这种方式的优势是捕获异常，当前线程可以返回；在异常中处理超时
        /// 当前的方式，task.Result会永久阻塞，如果底层一直不返回，就会一直占用2线程
        /// 比起捕获异常的方式会多占用一个线程，但是执行过程更加直观
        /// 这里不采用捕获异常的方式，认为大多数情况执行线程不会一直阻塞。
        /// 
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="address"></param>
        private void ProcessMonitor(DBTransfer transfer, EndPoint address)
        {
            long rspid= Interlocked.Increment(ref ResponeID);
            CancellationTokenSource cts = null;
            if (transfer.TimeOut == 0)
            {
                cts = new CancellationTokenSource();
            }
            else if (transfer.TimeOut == -1)
            {
                cts = new CancellationTokenSource(Timeout);
            }
            else if (transfer.TimeOut > 0)
            {
                cts = new CancellationTokenSource(transfer.TimeOut*1000);
            }
            var cancellation = cts.Token.Register(() =>
            {
                dicTimeOut[rspid] = DateTime.Now;
                TimeOutProcesser(transfer.RequestID, rspid, address);
               
            });
            var task=  Task.Factory.StartNew(() =>
            {
                return  Processor(rspid,transfer);
            },cts.Token);
           // task.Wait(cts.Token);
            var result=  task.Result;
            DateTime date;
            if(dicTimeOut.TryRemove(rspid,out date))
            {
                //说明已经做了超时处理
                return;
            }
            cancellation.Dispose();
            byte[] r = SerializerFactory<CommonSerializer>.Serializer<RequestResult>(result);
            AsyncUdpUserToken token = new AsyncUdpUserToken();
            token.Data = r;
            token.Length = r.Length;
            token.Remote = address;
            udp.SendPackage(token);
            Console.WriteLine("回复：" + r.Length);
        }

        /// <summary>
        /// 超时处理
        /// </summary>
        /// <param name="RequestID"></param>
        /// <param name="address"></param>
        private void TimeOutProcesser(long RequestID,long rspid,EndPoint address)
        {
            RequestResult result = new RequestResult();
            result.Error = ErrorCode.TimeOut;
            result.ErrorMsg = result.Error.ToDescriptionString();
            result.ReslutMsg = "执行超时";
            result.Result = null;
            result.RequestID = RequestID;
            result.ID = rspid;
            byte[] r = SerializerFactory<CommonSerializer>.Serializer<RequestResult>(result);
            AsyncUdpUserToken token = new AsyncUdpUserToken();
            token.Data = r;
            token.Length = r.Length;
            token.Remote = address;
            udp.SendPackage(token);
        }



       /// <summary>
       /// 执行
       /// </summary>
       /// <param name="transfer"></param>
       /// <returns></returns>
        private RequestResult Processor(long rspid,DBTransfer transfer)
        {
            bool r=semaphore.WaitOne(1000);//不完全阻塞
            if(!r)
            {
                CheckTimeOut();
            }
            dicExecute[rspid] = DateTime.Now;
            RequestResult result = null;
            if (transfer != null)
            {
                result = server.execete(rspid,transfer);
            }
            else
            {
                result = new RequestResult();
                result.Error = ErrorCode.Exception;
                result.ErrorMsg = result.Error.ToDescriptionString();
                result.ReslutMsg = "接收请求错误，无法转换DBTransfer";
            }
            semaphore.Release();
            DateTime date;
            dicExecute.TryRemove(rspid, out date);
            return result;
        }

        /// <summary>
        /// 监测
        /// </summary>
        private void CheckTimeOut()
        {
            if(isCheck)
            {
                return;
            }
            isCheck = true;
            Task.Factory.StartNew(() =>
            {
                int num = 0;
                if (dicTimeOut.Count > MaxThreadNum / 2)
                {
                    //占用一半，监测超时数据量
                    foreach (KeyValuePair<long, DateTime> kv in dicExecute)
                    {
                        if ((DateTime.Now - kv.Value).TotalSeconds > MaxExecuteTime)
                        {
                            num++;
                        }
                    }
                }
                if (num > MaxThreadNum / 2)
                {
                    //软件异常,有大量线程阻塞无法执行
                    Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NWatcher.exe"));
                }
                isCheck = false;
            });
        }
       
    }
}
