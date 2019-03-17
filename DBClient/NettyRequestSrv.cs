#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DBClient
* 项目描述 ：
* 类 名 称 ：AsynchronousStream
* 类 描 述 ：
* 命名空间 ：DBClient
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 16:25:07
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using NettyTransmission;
using System;

namespace DBClient
{
    /* ============================================================================== 
* 功能描述：NettyRequestSrv 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class NettyRequestSrv
    {
        private static Lazy<NettyRequestSrv> Instance = new Lazy<NettyRequestSrv>();

        public static NettyRequestSrv Singleton
        {
            get { return Instance.Value; }
        }
        private string host = "127.0.0.1";
        private NettyClient MqClient = null;
        private int port = 7777;

        public  string Host { get { return host; } set { host = value; } }

        public int Port { get { return port; } set { port = value; } }

        /// <summary>
        /// 请求获取
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        internal byte[] Request(byte[] req)
        {
             NettyClient client = new NettyClient();
             client.Start(host, port);
             client.Send(req);
             var result= client.GetData().Message as byte[];
             client.Close();
             return result;
        }

      
        internal byte[] KeepRequest(byte[] req)
        {
            if (MqClient == null)
            {
                MqClient = new NettyClient();
                MqClient.Start(host, port);
            }
             MqClient.Send(req);
            return MqClient.GetData().Message as byte[];
        }


        internal void KeepClose()
        {
            MqClient.Close();
        }

    }
}
