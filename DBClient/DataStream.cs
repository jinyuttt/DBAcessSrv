using DBModel;
using Serializer;

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
