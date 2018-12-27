using System.IO;
using ZMQNetSocket;
using DBModel;
using Serializer;
using DBServer;
using System.Threading;
using ExecutorService;
using System.Threading.Tasks;
using NetMQ.Sockets;
using NetMQ;
using System.Text;
using System;
using ExecuteErrorCode = ExecutorService.ErrorCode;
using ResultErrorCode = DBModel.ErrorCode;
using System.Data;

namespace NetCSDB
{
    public class DBAcessServer
    {
        private string address = "127.0.0.1:7777";
        private readonly string backAdress = "inproc://backend";
        private const int TimeOut = 5000;//5秒
        private long rspID = 0;
        private ZMQServer server = null;
        private ProxyZSocket proxy = new ProxyZSocket();
        private DBAcessSrv dBAcess = null;
        private object obj_lock = new object();
        public void Start()
        {
            ReadConfig();
            server = new ZMQServer();
            Thread dbRevice = new Thread(() =>
              {
                  proxy.Bind("tcp://" + address, backAdress);
                  server.Rsp(backAdress);
              });
            dbRevice.IsBackground = true;
            dbRevice.Name = "dbTCP";
            dbRevice.Start();

            Revice();
        }

        private void Revice()
        {
            Thread dbRevice = new Thread(() =>
            {
                while (true)
                {
                    var item = server.GetTCPUserToken();
                    Process(item);
                }
            });
            dbRevice.IsBackground = true;
            dbRevice.Name = "dbAcess";
            dbRevice.Start();
        }
        private void ReadConfig()
        {
            string path = Path.Combine("Config", "Server.cfg");
            using (StreamReader rd = new StreamReader(path))
            {
                string line=rd.ReadLine();
                if(!string.IsNullOrEmpty(line))
                {
                    address = line.Trim();
                }
            }
        }

        private void Process(TCPUserToken token)
        {
          if(dBAcess==null)
            {
                lock (obj_lock)
                {
                    if (dBAcess == null)
                    {
                        dBAcess = new DBAcessSrv();
                    }
                }
            }
            Task.Factory.StartNew((req) =>
            {
                TCPUserToken userToken = req as TCPUserToken;
                DBTransfer model = SerializerFactory<CommonSerializer>.Deserialize<DBTransfer>(userToken.Data);
              
                RequestResult result = null;
                if (model.TimeOut == 0)
                {
                    //不超时
                     result = dBAcess.Execete(Interlocked.Increment(ref rspID), model);
                }
                else
                {
                    int timeOut = model.TimeOut <0 ? TimeOut : model.TimeOut*1000;
                    var taskResult = Executors.Submit(() =>
                    {
                        return  dBAcess.Execete(Interlocked.Increment(ref rspID), model);
                    }, timeOut);
                    result = taskResult.Result;
                    if(result==null)
                    {
                        result = new RequestResult();
                    }
                    if(ExecuteErrorCode.timeout==taskResult.ResultCode)
                    {
                        result.Error = ResultErrorCode.TimeOut;
                    }
                    else if(ExecuteErrorCode.exception==taskResult.ResultCode)
                    {
                        result.Error = ResultErrorCode.Exception;
                    }
                }
                //
                if(model.IsJson&&result.Error==DBModel.ErrorCode.Sucess&&model.IsQuery)
                {
                    CommonSerializer common = new CommonSerializer();
                    result.Result = common.JSONObjectToString(result.Result);
                }
                byte[] buffer = null;
                try
                {
                    if(result.Result is DataTable)
                    {
                        DataTable dt = result.Result as DataTable;
                        result.Result = dt.DataSet.GetXml();
                    }
                   buffer = SerializerFactory<CommonSerializer>.Serializer(result);

                }
                catch(Exception ex)
                {
                    result.Error = ResultErrorCode.Exception;
                    result.ReslutMsg = "序列化失败," + ex.Message;
                    result.Result = null;
                    buffer = SerializerFactory<CommonSerializer>.Serializer(result);

                }
                userToken.Rsp(buffer);
            }, token);
           
        }
       

    }
}
