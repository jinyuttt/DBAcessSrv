
#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：NettyClient
* 类 描 述 ：
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 3:58:07
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using DotNetty.Codecs;
using System.Threading;
using DotNetty.Logging;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：NettyClient 客户端连接
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class NettyClient
    {

        MultithreadEventLoopGroup group = null;
        IChannel clientChannel = null;
        private SimpleClientHandler simpleClient = null;
        private readonly BlockingCollection<SrvDataSource> queue = new BlockingCollection<SrvDataSource>();
        private AutoResetEvent resetEvent = null;
        private volatile bool isConnected = false;
        private  long conTime = 0;

        /// <summary>
        /// 是否已经连接
        /// </summary>
        public bool IsConnected
        {
            get { return isConnected&& simpleClient !=null&& simpleClient.IsConnect; }
        }

        /// <summary>
        /// 无法连接客户端
        /// </summary>
        public bool IsUnConnect
        {
            get;set;
        }

        /// <summary>
        /// 连接时间
        /// </summary>
        public long ConnecTime { get { return conTime; } }

        /// <summary>
        /// 数据回调
        /// </summary>
        public event NettyDataNotify DataNotify = null;
     
        /// <summary>
        /// 启动连接
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public async Task Start(string host,int port)
        {
            group = new MultithreadEventLoopGroup();
            resetEvent = new AutoResetEvent(false);
            X509Certificate2 cert = null;
            string targetHost = null;
            if (ClientSettings.IsSsl)
            {
                cert = new X509Certificate2(Path.Combine("", "dotnetty.com.pfx"), "password");
                targetHost = cert.GetNameInfo(X509NameType.DnsName, false);
            }
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        if (cert != null)
                        {
                            pipeline.AddLast("tls", new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(targetHost)));
                        }
                        pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        simpleClient = new SimpleClientHandler();
                        pipeline.AddLast("echo", simpleClient);
                        simpleClient.DataNotify += SimpleClient_DataNotity;
                    }));
                 conTime = DateTime.Now.Ticks;
                 Console.WriteLine("客户端连接中");
                 clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(host),port));
                 isConnected = true;


            }
            catch
            {
                IsUnConnect = true;
                Logger.Singleton.ErrorFormat("客户端无法连接成功,服务端IP{0},端口{1}", host, port);
                if (clientChannel != null)
                    await clientChannel.CloseAsync();
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        private void SimpleClient_DataNotity(object sender, object msg,string flage=null)
        {
            if (DataNotify != null)
            {
                DataNotify(this, msg, flage);
                Console.WriteLine("客户端推送数据");
            }
            else
            {
                SrvDataSource source = new SrvDataSource() { Context = sender, Message = msg };
                queue.Add(source);
            }
        }

        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            Console.WriteLine("NettyClient关闭");
            isConnected = false;
            queue.Dispose();
            if (simpleClient != null)
            {
                simpleClient.DataNotify -= SimpleClient_DataNotity;
                simpleClient = null;
            }
            if (clientChannel != null)
            {
                await clientChannel.CloseAsync();
               
            }
            await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            clientChannel = null;
            group = null;
        }
        
        public void Send(byte[] data)
        {
            simpleClient.Send(data);
        }


        /// <summary>
        /// 同步获取数据
        /// </summary>
        /// <returns></returns>
        public SrvDataSource GetData()
        {
            return queue.Take();
        }
    }
}
