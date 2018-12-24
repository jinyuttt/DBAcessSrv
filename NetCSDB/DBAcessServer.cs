using System.IO;
using ZMQNetSocket;
using DBModel;
using Serializer;
using DBServer;
using System.Threading;
using ExecutorService;
using System.Threading.Tasks;
namespace NetCSDB
{
    public class DBAcessServer
    {
        private string address = "*:7777";
        private const int TimeOut = 5000;//5秒
        private long rspID = 0;
        ZMQServer server = null;
        public void Start()
        {
            ReadConfig();
            server = new ZMQServer();
            Thread dbRevice = new Thread(() =>
              {
                  
                  server.Start(address);
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
          
            Task.Factory.StartNew((req) =>
            {
                TCPUserToken userToken = req as TCPUserToken;
                DBTransfer model = SerializerFactory<CommonSerializer>.Deserialize<DBTransfer>(userToken.Data);
                DBAcessSrv dBAcess = new DBAcessSrv();
                RequestResult result = null;
                if (model.TimeOut == 0)
                {
                    //不超时
                     result = dBAcess.Execete(Interlocked.Increment(ref rspID), model);
                }
                else
                {
                    int timeOut = model.TimeOut <0 ? TimeOut : model.TimeOut;
                    var taskResult = Executors.Submit(() =>
                    {
                        return  dBAcess.Execete(Interlocked.Increment(ref rspID), model);
                    }, timeOut);
                    result = taskResult.Result;
                    if(result==null)
                    {
                        result = new RequestResult();
                    }
                    if(ExecutorService.ErrorCode.timeout==taskResult.ResultCode)
                    {
                        result.Error = DBModel.ErrorCode.TimeOut;
                    }
                    else if(ExecutorService.ErrorCode.exception==taskResult.ResultCode)
                    {
                        result.Error = DBModel.ErrorCode.Exception;
                    }
                }
                //
                if(model.IsJson&&result.Error==DBModel.ErrorCode.Sucess&&model.IsQuery)
                {
                    CommonSerializer common = new CommonSerializer();
                    result.Result = common.JSONObjectToString(result.Result);
                }
                byte[] buffer = SerializerFactory<CommonSerializer>.Serializer(result);
                userToken.Rsp(buffer);
            }, token);
           
        }
       

    }
}
