using DBModel;
using NettyTransmission;
using Serializer;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace DBClient
{

    /// <summary>
    /// 数据网络操作
    /// </summary>
    public class DataStream
    {
        public static readonly DataStream Instance = new DataStream();
        private long TaskID = 0;

        public DataStream()
        {
            Init();
        }
        private void Init()
        {
            var lst= SrvControl.Instance.AllAddress;
            var addr= NettyAddressReader.ReaderClient(lst);
            DataClientAdapter.AddSeverAddress(addr);
           
        }

       /// <summary>
       /// 发送请求带返回
       /// </summary>
       /// <typeparam name="T"></typeparam>
       /// <param name="transfer"></param>
       /// <returns></returns>
        public T  Send<T>(DBTransfer transfer)
        {
            byte[] buf = SerializerFactory<CommonSerializer>.Serializer(transfer);
            //RequestServer request = new RequestServer();
            //request.Address = SrvControl.Instance.GetCureent();
            //byte[] rec= request.Request(buf);
            byte[] rec = SetRequest(transfer);
           
            if (rec==null)
            {
                return default(T);
            }
            RequestResult result = SerializerFactory<CommonSerializer>.Deserialize<RequestResult>(rec);
            if (result.Error == ErrorCode.Sucess)
            {
                if(typeof(T)==typeof(DataTable)|| typeof(T) == typeof(DataSet))
                {
                    //当前序列化不能对DataTable
                    //服务端转换为XML
                    if (result.Result != null)
                    {
                        DataSet ds = new DataSet();
                        byte[]bytes=SerializerFactory<CommonSerializer>.Serializer(result.Result);
                        DataSetModel dataSetModel = SerializerFactory<CommonSerializer>.Deserialize<DataSetModel>(bytes);
                        if (dataSetModel != null)
                        {
                            var stream = new StringReader(dataSetModel.Schema);
                            ds.ReadXmlSchema(stream);
                            //ds.ReadXml(dataSetModel.Content);
                            //从stream装载到XmlTextReader
                            stream = new StringReader(dataSetModel.Content);
                            var reader = new XmlTextReader(stream);
                            ds.ReadXml(reader);
                            if (typeof(T) == typeof(DataSet))
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
            else if(result.ReslutMsg=="请登录")
            {
                CryptoClient.Singleton.IsLogin = false;
                throw new ServerException(result.ReslutMsg);
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
            //RequestServer request = new RequestServer();
            //request.Address = SrvControl.Instance.GetCureent();
            //byte[] rec = request.Request(buf);
            byte[] rec = SetRequest(transfer);
            if(rec==null)
            {
                return;
            }
            RequestResult result = SerializerFactory<CommonSerializer>.Deserialize<RequestResult>(rec);
            if (result.Error == ErrorCode.Exception)
            {
                 if (result.ReslutMsg == "请登陆")
                {
                    CryptoClient.Singleton.IsLogin = false;
                }
                else
                {
                    throw new ServerException(result.ReslutMsg);
                }
            }
           
        }

        /// <summary>
        /// 注入执行队列异步执行
        /// 需要执行结果注册RequestQueue单例事件
        /// </summary>
        /// <param name="transfer"></param>
        public void Push(DBTransfer transfer)
        {
            // RequestQueue.Instance.Push(transfer);
            // DataClientAdapter.Singleton.Request(rec);
            SendRequest(transfer);
        }
        

        private void SendRequest(DBTransfer transfer)
        {
            string AesKey = null;
            byte[] req = SerializerFactory<CommonSerializer>.Serializer(transfer);
            byte[] keys = CryptoClient.Singleton.EncryptAESKey(out AesKey);//获取AES秘钥，并且加密
            byte[] data = CryptoClient.Singleton.Encrypt(req, AesKey);
            byte[] buf = new byte[data.Length + keys.Length + 24 + 1];//一个标志位
            transfer.RequestID = Interlocked.Increment(ref TaskID);
            //复制数据
            using (var mem = new MemoryStream(buf))
            {
                mem.Position = 1;
                mem.Write(BitConverter.GetBytes(data.Length + keys.Length), 0, 4);
                mem.Write(BitConverter.GetBytes(keys.Length), 0, 4);
                mem.Write(BitConverter.GetBytes(CryptoClient.Singleton.Sessionid), 0, 8);
                mem.Write(BitConverter.GetBytes(transfer.RequestID), 0, 8);
                mem.Write(data, 0, data.Length);
                mem.Write(keys, 0, keys.Length);
            }
            Console.WriteLine("加密前:{0},加密后:{1}", req.Length, data.Length);
            //var reslut=  NettyRequestSrv.Singleton.Request(buf);
            DataClientAdapter.Singleton.Push(req);
        }

        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private byte[] SetRequest(DBTransfer transfer)
        {
            int num = 0;
            while(!CryptoClient.Singleton.IsLogin)
            {
                Login();
                Thread.Sleep(1000);
                num++;
                if(num>10)
                {
                    break;
                }
            }
            if(!CryptoClient.Singleton.IsLogin)
            {
                Console.WriteLine("登录失败");
                return  null;
            }
            //传输：数据总长， AES秘钥长度，sessionid，taskID
            //加密数据+AESKEY
            string AesKey = null;
            byte[] req = SerializerFactory<CommonSerializer>.Serializer(transfer);
            byte[] keys= CryptoClient.Singleton.EncryptAESKey(out AesKey);//获取AES秘钥，并且加密
            byte[] data = CryptoClient.Singleton.Encrypt(req, AesKey);
            byte[] buf = new byte[data.Length + keys.Length + 24+1];//一个标志位
            transfer.RequestID = Interlocked.Increment(ref TaskID);
            //复制数据
            using (var mem = new MemoryStream(buf))
            {
                mem.Position = 1;
                mem.Write(BitConverter.GetBytes(data.Length + keys.Length), 0, 4);
                mem.Write(BitConverter.GetBytes(keys.Length), 0, 4);
                mem.Write(BitConverter.GetBytes(CryptoClient.Singleton.Sessionid), 0, 8);
                mem.Write(BitConverter.GetBytes(transfer.RequestID), 0,8);
                mem.Write(data, 0, data.Length);
                mem.Write(keys, 0, keys.Length);
            }
            Console.WriteLine("加密前:{0},加密后:{1}", req.Length, data.Length);
            //var reslut=  NettyRequestSrv.Singleton.Request(buf);
            var result= DataClientAdapter.Singleton.Request(buf);
            //
            if(Enumerable.SequenceEqual(buf, result))
            {
                //说明网络层无法传递，原样返回了。
                throw new Exception("网络异常,无法传输");
            }
            return CryptoClient.Singleton.Decrypt(result, AesKey);

        }

        /// <summary>
        /// 设置登陆
        /// </summary>
        public void Login()
        {
            var r=CryptoClient.Singleton.LoginSrv();
            var rsp = DataClientAdapter.Singleton.Request(r);
          //  var rsp = NettyRequestSrv.Singleton.Request(r);
            CryptoClient.Singleton.SetLogin(rsp);
        }
    }
}
