#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCSDB
* 项目描述 ：
* 类 名 称 ：NettyDBAcessServer
* 类 描 述 ：
* 命名空间 ：NetCSDB
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 18:09:59
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using CryptoStruct;
using DBModel;
using DBServer;
using NettyTransmission;
using Serializer;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NetCSDB
{
    /* ============================================================================== 
* 功能描述：NettyDBAcessServer 服务请求
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class NettyDBAcessServer
    {
       
        private const int TimeOut = 5000;//5秒
        private long rspID = 0;
        private System.Timers.Timer  sessionTimer = null;
        private List<NettyAddress> LstAddres = new List<NettyAddress>();
        /// <summary>
        /// 读取配置
        /// </summary>
        private void ReadConfig()
        {

            LstAddres= NettyAddressReader.ReaderSrvFile();
        }


        /// <summary>
        /// 启动
        /// </summary>
        /// <returns></returns>
        public async Task Start()
        {
            ReadConfig();
            //

            DataSrvAdapter.Addresses = LstAddres;
            DataSrvAdapter.Singleton.SrvDataNotify += Singleton_SrvDataNotify;
            //
            sessionTimer = new System.Timers.Timer();
            sessionTimer.Interval = 1000 * 60 * 60;
            sessionTimer.Enabled = true;
            sessionTimer.Elapsed += SessionTimer_Elapsed;
        }

        private void Singleton_SrvDataNotify(object sender, object msg, string flage = null)
        {
            //处理请求
            if (msg is SrvDataSource)
            {
                AnalysisRequest(msg as SrvDataSource);
            }
            else
            {
                AnalysisRequest(new SrvDataSource() { Context = sender, Message = msg });
            }
        }

        private void SessionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
           
            //定时要求登录
            long[] session = new long[CipherReply.Singleton.Session.Count];
            CipherReply.Singleton.Session.Keys.CopyTo(session, 0);
            DateTime cur;
            int curDay = DateTime.Now.Day;
            foreach(long id in session)
            {
                if(CipherReply.Singleton.Session.TryGetValue(id,out cur))
                {
                    if(cur.Day!=curDay)
                    {
                        CipherReply.Singleton.Session.Remove(id);
                    }
                }
            }
        }

        

        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="source"></param>
        private void AnalysisRequest(SrvDataSource source)
        {
            byte[] req = source.Message as byte[];
            //ID,数据，AES
            if (req[0]==1)
            {
                //登陆请求
             
                byte[] bytes = new byte[req.Length-1];
                Array.Copy(req, 1, bytes, 0,bytes.Length);
                ClientLoginRequest request= StructManager.BytesToStruct<ClientLoginRequest>(bytes);
                var rsp= CryptoServer.Singleton.ResponseLogin(request, "");
                var  result= SerializerFactory<CommonSerializer>.Serializer(rsp);
             
                source.Rsponse(result);
            }
            else
            {
                byte[] bytes = new byte[req.Length - 1];
                Array.Copy(req, 1, bytes, 0, bytes.Length);
                //解析客户端数据
                //获取解密后的数据已经AES秘钥
                var  creq= CryptoServer.Singleton.ProcessRequest(bytes);
                source.Message = creq.data;
                creq.data = null;
                ProcessClient(source,creq);
            }
        }


        /// <summary>
        /// 处理数据
        /// </summary>
        /// <param name="source"></param>
        private void ProcessClient(SrvDataSource source,ClientRequest client)
        {
            if (!CipherReply.Singleton.Session.ContainsKey(client.SessionID))
            {
                //已经离线处理
                LoginOut(source, client.TaskID, client.AESKey);
            }
            else
            {
                //开启线程处理，不影响接收
                Task.Factory.StartNew(() =>
                {
                    SubmitTask(source, client);
                 });
            }
        }

        /// <summary>
        /// 处理请求任务
        /// </summary>
        /// <param name="source"></param>
        /// <param name="client"></param>
        private void SubmitTask(SrvDataSource source, ClientRequest client)
        {
            Console.WriteLine("Submint:"+Thread.CurrentThread.ManagedThreadId);
            var data = source.Message as byte[];
            //获取传递的请求
            DBTransfer model = SerializerFactory<CommonSerializer>.Deserialize<DBTransfer>(data);

            if (model.TimeOut == 0)
            {
                //不超时
                var result = DBAcessSrv.Singleton.Execete(Interlocked.Increment(ref rspID), model);
                ResponseResult(source, result, model, client);
            }
            else
            {
                //超时请求
                var result = ExecuteTimeOut(source, client, model);
                ResponseResult(source, result, model, client);
            }
        
        }

        /// <summary>
        /// 监视超时处理
        /// </summary>
        /// <param name="source"></param>
        /// <param name="client"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private RequestResult ExecuteTimeOut(SrvDataSource source, ClientRequest client, DBTransfer model)
        {
             int timeOut = model.TimeOut < 0 ? TimeOut : model.TimeOut * 1000;//转换成毫秒
             ManualResetEventSlim slim = new ManualResetEventSlim(false);
             RequestResult result = null;
             ThreadPool.QueueUserWorkItem((item) =>
              {
                  //提交线程池
                  result = DBAcessSrv.Singleton.Execete(Interlocked.Increment(ref rspID), model);
                  slim.Set();//设置
              });
            if (!slim.Wait(timeOut))
            {
                //等待超时时间
                result = new RequestResult();
                result.Error = ErrorCode.TimeOut;
                result.RequestID = client.TaskID;
            }
            return result;
        }


        /// <summary>
        /// 登录重置
        /// </summary>
        /// <param name="source"></param>
        /// <param name="taskID"></param>
        /// <param name="aesKey"></param>
        private void LoginOut(SrvDataSource source,long taskID,string aesKey)
        {
            RequestResult result = new RequestResult();
            result.Error = ErrorCode.Exception;
            result.ReslutMsg = "请登录";
            result.Result = null;
            result.ID = Interlocked.Increment(ref rspID);
            result.RequestID = taskID;

            byte[] buffer = SerializerFactory<CommonSerializer>.Serializer(result);
            buffer = CryptoServer.Singleton.EnCrypto(buffer, aesKey);//AES 加密回传
            
            source.Rsponse(buffer);
        }

        /// <summary>
        /// 准备数据回传
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="result"></param>
        /// <param name="model"></param>
        /// <param name="client"></param>
        private void ResponseResult(SrvDataSource source, RequestResult result, DBTransfer model,ClientRequest client)
        {
            result.RequestID = client.TaskID;
            result.ID = client.SessionID;
            if (model.IsJson && result.Error == ErrorCode.Sucess && model.IsQuery)
            {
                CommonSerializer common = new CommonSerializer();
                result.Result = common.JSONObjectToString(result.Result);
            }
            byte[] buffer = null;
            try
            {
                if (result.Result is DataTable)
                {
                    DataTable dt = result.Result as DataTable;
                    DataSetModel dataSet = new DataSetModel() { Content = dt.DataSet.GetXml(), Schema = dt.DataSet.GetXmlSchema() };
                    result.Result = dataSet;
                }
                buffer = SerializerFactory<CommonSerializer>.Serializer(result);

            }
            catch (Exception ex)
            {
                result.Error = ErrorCode.Exception;
                result.ReslutMsg = "序列化失败," + ex.Message;
                result.Result = null;
                buffer = SerializerFactory<CommonSerializer>.Serializer(result);
            }
           
            buffer= CryptoServer.Singleton.EnCrypto(buffer, client.AESKey);//AES 加密回传
            //source.Context.Send(buffer)
            source.Rsponse(buffer);
            Console.WriteLine("回传:" + buffer.Length);
        }
    }


    public class TimeOutState
    {
        public RequestResult Result { get; set; }

        public bool IsTimeOut { get; set; }
    }

}
