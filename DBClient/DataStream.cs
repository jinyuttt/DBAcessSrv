using DBModel;
using Serializer;
using System.Data;
using System.IO;
using System.Xml;

namespace DBClient
{
    public class DataStream
    {
        public static readonly DataStream Instance = new DataStream();

       /// <summary>
       /// 发送请求带返回
       /// </summary>
       /// <typeparam name="T"></typeparam>
       /// <param name="transfer"></param>
       /// <returns></returns>
        public T  Send<T>(DBTransfer transfer)
        {
            byte[] buf = SerializerFactory<CommonSerializer>.Serializer(transfer);
            RequestServer request = new RequestServer();
            request.Address = SrvControl.Instance.GetCureent();
            byte[] rec= request.Request(buf);
            RequestResult result = SerializerFactory<CommonSerializer>.Deserialize<RequestResult>(rec);
            if (result.Error != ErrorCode.Exception)
            {
                if(typeof(T)==typeof(DataTable)|| typeof(T) == typeof(DataSet))
                {
                    //当前序列化不能对DataTable
                    //服务端转换为XML
                    if (result.Result != null)
                    {
                        if(typeof(string) == result.Result.GetType())
                        {
                            DataSet ds = new DataSet();
                            var  stream = new StringReader(result.Result.ToString());
                            //从stream装载到XmlTextReader
                            var reader = new XmlTextReader(stream);
                            ds.ReadXml(reader);
                            if(typeof(T) == typeof(DataSet))
                            {
                                result.Result = ds;
                            }
                            else
                            {
                                result.Result = ds.Tables[0];
                            }
                        }
                
                    }
                }
                return (T)result.Result;
            }
            else
            {
                throw new ServerException(result.ReslutMsg);
            }
        }

        /// <summary>
        /// 发送请求无返回
        /// </summary>
        /// <param name="transfer"></param>
        public void Send(DBTransfer transfer)
        {
            byte[] buf = SerializerFactory<CommonSerializer>.Serializer(transfer);
            RequestServer request = new RequestServer();
            request.Address = SrvControl.Instance.GetCureent();
            byte[] rec = request.Request(buf);
            RequestResult result = SerializerFactory<CommonSerializer>.Deserialize<RequestResult>(rec);
            if (result.Error == ErrorCode.Exception)
            {
                throw new ServerException(result.ReslutMsg);
            }
           
        }

        /// <summary>
        /// 注入执行队列异步执行
        /// 需要执行结果注册RequestQueue单例事件
        /// </summary>
        /// <param name="transfer"></param>
        public void Push(DBTransfer transfer)
        {
            RequestQueue.Instance.Push(transfer);
        }
        
    }
}
