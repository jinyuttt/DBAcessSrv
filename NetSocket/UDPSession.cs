/**
* 命名空间: NetSocket 
* 类 名：UDPSession 
* CLR版本： 4.0.30319.42000
* 版本 ：v1.0
* Copyright (c) jinyu  
*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace NetSocket
{

    /// <summary>
    /// 功能描述    ：UDPSession 数据协议分包，同时简单的重传
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018
    /// </summary>
  public  class UDPSession
    {
        UDPPack uDPPack = null;

        public bool EnableHeart { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 默认65535
        /// bind之前设置
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// 缓存区间最大大小(M或者个数)
        /// 默认1024
        /// 这里是2个相同的缓存区；合理设置
        /// </summary>
        public int TotalBufSize { get; set; }
      
        /// <summary>
        /// 缓存数据实体
        /// 默认100
        /// </summary>
        public int TokenMaxLeftNum { get; set; }

        public event OnReceiveUdpData OnDataReceived;

        private const int MByte = 1024 * 1024;//运算

        private const int ValidateTime = 10;//验证每个端点的
         private   AutoResetEvent resetEvent = null;//控制端口验证，关闭时可以快速退出
         private  volatile bool isValidatePoint = true;


        /// <summary>
        /// 发送队列
        /// </summary>
        ConcurrentDictionary<long, SendQueue> dicSendQueue = null;

        private SocketEndPoint socketEndPoint = null;

        public UDPSession()
        {
            BufferSize = 65535;
            TotalBufSize = 1024;
            TokenMaxLeftNum = Environment.ProcessorCount * 10;
            if (TokenMaxLeftNum == 0)
            {
                TokenMaxLeftNum = 100;
            }
            socketEndPoint = new SocketEndPoint();
            resetEvent = new AutoResetEvent(false);

        }

        /// <summary>
        /// 绑定方法，必须调用
        /// </summary>

        public void Bind()
        {
            uDPPack = new UDPPack();
            uDPPack.BufferSize = BufferSize;
            uDPPack.EnableHeart = EnableHeart;
            uDPPack.Host = Host;
            uDPPack.IsFixCache = false;
            uDPPack.Port = Port;
            uDPPack.TotalBufSize = TotalBufSize;
            uDPPack.TokenMaxLeftNum = TokenMaxLeftNum;
            uDPPack.IsProtolUnPack = false;
            uDPPack.Bind();
            uDPPack.OnDataReceived += UDPSocket_OnDataReceived;
            socketEndPoint.OnDataReceived += SocketEndPoint_OnDataReceived;
            socketEndPoint.OnLossData += SocketEndPoint_OnLossData;
            StartReceive();

        }

        private void SocketEndPoint_OnLossData(object sender,object remote, LosPackage[] list)
        {
            AsyncUdpUserToken token = new AsyncUdpUserToken();
            IPEndPoint endPoint = remote as IPEndPoint;
            token.Remote = endPoint;
            foreach (LosPackage los in list)
            {
                los.Pack();
                token.Data = los.PData;
                uDPPack.Send(token, 0);

            }
        }

        /// <summary>
        /// 接收完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="token"></param>
        private void SocketEndPoint_OnDataReceived(object sender, AsyncUdpUserToken token)
        {
            if (OnDataReceived != null)
            {
                OnDataReceived(this, token);
            }
        }

        /// <summary>
        /// 接收底层的数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="token"></param>
        private void UDPSocket_OnDataReceived(object sender, AsyncUdpUserToken token)
        {
            Console.WriteLine("接收数据个数:" + token.Length);
            switch (token.Data[token.Offset])
            {
                
                case 0:
                    {
                        //数据
                        socketEndPoint.Add(token);
                        if(isValidatePoint)
                        {
                            isValidatePoint = false;
                            EndPointValidate();
                        }
                    }
                    break;
                case 1:
                    {
                        //接收完成序列
                        Console.WriteLine("接收小包完成返回");
                        SendQueue sendQueue = null;
                        LosPackage rsp = new LosPackage(token.Data);
                        if (dicSendQueue.TryGetValue(rsp.packageID,out sendQueue))
                        {
                            sendQueue.Add(rsp.packageSeq);
                        }
                       

                    }
                    break;

                case 2:
                    {
                        //丢失序列
                        Console.WriteLine("接收丢失请求");
                        SendQueue sendQueue = null;
                        LosPackage rsp = new LosPackage(token.Data);
                        if (dicSendQueue.TryGetValue(rsp.packageID, out sendQueue))
                        {
                            AsyncUdpUserToken  resend=  sendQueue.GetAsyncUdpUserToken(rsp.packageSeq);
                            if(resend!=null)
                             uDPPack.Send(resend, 0);
                        }
                    }
                    break;
                case 3:
                    {
                        //完成接收
                     
                        SendQueue sendQueue = null;
                        LosPackage rsp = new LosPackage(token.Data);
                        if (dicSendQueue.TryRemove(rsp.packageID, out sendQueue))
                        {
                            sendQueue.Clear();
                        }
                        Console.WriteLine("接收完成返回:"+rsp.packageID);
                    }
                    break;
            }
           

        }

       

        /// <summary>
        /// 接收
        /// </summary>
        private void StartReceive()
        {
            uDPPack.StartReceive();
        }

      

        /// <summary>
     /// 发送数据
     /// </summary>
     /// <param name="token"></param>
        public void SendPackage(AsyncUdpUserToken token)
        {
            if(uDPPack==null)
            {
                Bind();
            }
            //使用了分包缓存
            token.ListPack = new List<AsyncUdpUserToken>();
            //
            uDPPack.SendProtol(token);
            //
            if (dicSendQueue == null)
            {
                dicSendQueue = new ConcurrentDictionary<long, SendQueue>();
            }
            SendQueue sendList = new SendQueue(token);
            dicSendQueue[token.DataPackage.packageID] = sendList;
            sendList.PushLossReset += SendList_PushLossReset;
        }


        /// <summary>
        /// 重发丢失数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="list"></param>
        private void SendList_PushLossReset(object sender, AsyncUdpUserToken[] list)
        {


          //  Console.WriteLine("丢失重发");
            if (list.Length > 0)
            {
               //说明100ms没有收到
              //  Console.WriteLine("丢失重发：");

            }
            foreach (AsyncUdpUserToken token in list)
            {
                 uDPPack.Send(token, 0);
            }
            //SendQueue sendQueue = sender as SendQueue;
            //if(sendQueue!=null)
            //{
            //    //已经全部接收了，没有丢失
            //    sendQueue.Clear();
            //    sendQueue.PushLossReset -= SendList_PushLossReset;
            //}

        }


        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void SendPackage(byte[] data, string host, int port)
        {
            AsyncUdpUserToken token = new AsyncUdpUserToken();
            token.Data = data;
            token.Offset = 0;
            token.Length = data.Length;
            token.Remote = new IPEndPoint(IPAddress.Parse(host), port);
            SendPackage(token);
        }


        /// <summary>
        /// 关闭
        /// </summary>
        private void Close()
        {
            resetEvent.Set();
            uDPPack.Close();
            this.dicSendQueue.Clear();
            socketEndPoint.Clear();
            resetEvent.Set();
           
        }

        private void EndPointValidate()
        {
             Task.Factory.StartNew(() => {
                if(!resetEvent.WaitOne(ValidateTime * 60 * 1000))
                {
                    socketEndPoint.Validate();
                    EndPointValidate();
                }

            });
        }






    }
}
