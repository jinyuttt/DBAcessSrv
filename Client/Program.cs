using DBModel;
using NetSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serializer;
namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            while (true)
            {
                UDPSession session = new UDPSession();
                session.OnDataReceived += Session_OnDataReceived;
                DBTransfer transfer = new DBTransfer();
                transfer.DBServerType = DBServerType.ServerSQL;
                transfer.SQL = "select * from Student";
                transfer.RequestID = 0;//异步处理可以辨别
                transfer.IsQuery = true;
               // transfer.DBCfg = "XXXX";//服务端SQL支持多数据库，以配置文件名称区分调用的;不配置即是服务端默认值
                //transfer.IsScala = false;
                //transfer.IsModel = true;
                //transfer.ModelCls = "xxxxx.xxx";
                //transfer.ModelDLL = "xxx.dll";
                byte[] req = SerializerFactory<CommonSerializer>.Serializer(transfer);
                session.SendPackage(req, "127.0.0.1", 7777);
                Console.ReadLine();
            }
          
        }

        private static void Session_OnDataReceived(object sender, AsyncUdpUserToken token)
        {
            RequestResult result = SerializerFactory<CommonSerializer>.Deserialize<RequestResult>(token.Data);
            if(result!=null)
            {
                Console.WriteLine(result.ErrorMsg);
            }
        }
    }
}
