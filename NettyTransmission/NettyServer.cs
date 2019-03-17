#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：NettyServer
* 类 描 述 ：
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 3:19:17
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using DotNetty.Handlers.Timeout;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using DotNetty.Codecs;
using DotNetty.Transport.Libuv;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：NettyServer 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public  class NettyServer
    {
      
        IChannel boundChannel = null;
        MultithreadEventLoopGroup bossGroup = null;
        MultithreadEventLoopGroup workerGroup = null;
       // private SimpleServerHandler simpleServer = null;
        
       /// <summary>
       /// 事件接收数据
       /// </summary>
        public event NettyDataNotify DataNotify = null;

        /// <summary>
        /// 数据集合
        /// </summary>
        private readonly BlockingCollection<SrvDataSource> queue = new BlockingCollection<SrvDataSource>();

        /// <summary>
        /// 是否绑定成功
        /// </summary>
        public bool IsBinded { get; set; }

        /// <summary>
        /// 服务端标识
        /// </summary>
        public string NettySrvFlage { get; set; }

        public NettyServer()
        {
            IsBinded = false;
        }
        /// <summary>
        /// 开始
        /// </summary>
        /// <returns></returns>
        public async Task Start(int port,string host=null)
        {
            IEventLoopGroup bossGroup;
            IEventLoopGroup workerGroup;
            ServerSettings.UseLibuv = true;
            if (ServerSettings.UseLibuv)
            {
                var dispatcher = new DispatcherEventLoopGroup();
                bossGroup = dispatcher;
                workerGroup = new WorkerEventLoopGroup(dispatcher);
            }
            else
            {
                bossGroup = new MultithreadEventLoopGroup(1);
                workerGroup = new MultithreadEventLoopGroup();
            }

            X509Certificate2 tlsCertificate = null;
            if (ServerSettings.IsSsl) //如果使用加密通道
            {
                tlsCertificate = new X509Certificate2(Path.Combine("", "dotnetty.com.pfx"), "password");
            }
            try
            {

                //声明一个服务端Bootstrap，每个Netty服务端程序，都由ServerBootstrap控制，
                //通过链式的方式组装需要的参数

                var bootstrap = new ServerBootstrap();
               
                bootstrap .Group(bossGroup, workerGroup); // 设置主和工作线程组
                if (ServerSettings.UseLibuv)
                {
                    bootstrap.Channel<TcpServerChannel>();
                }
                else
                {
                    bootstrap.Channel<TcpServerSocketChannel>();
                }
                   bootstrap.Option(ChannelOption.SoBacklog, 100) // 设置网络IO参数等，这里可以设置很多参数，当然你对网络调优和参数设置非常了解的话，你可以设置，或者就用默认参数吧
                    .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                    { //工作线程连接器 是设置了一个管道，服务端主线程所有接收到的信息都会通过这个管道一层层往下传输
                      //同时所有出栈的消息 也要这个管道的所有处理器进行一步步处理
                        IChannelPipeline pipeline = channel.Pipeline;
                      
                        if (tlsCertificate != null) //Tls的加解密
                        {
                            pipeline.AddLast("tls", TlsHandler.Server(tlsCertificate));
                        }
                        pipeline.AddLast("timeout", new IdleStateHandler(0, 0, 60));//60秒
                        //
                      
                        //日志拦截器
                      
                        //出栈消息，通过这个handler 在消息顶部加上消息的长度
                         pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                        //入栈消息通过该Handler,解析消息的包长信息，并将正确的消息体发送给下一个处理Handler，该类比较常用，后面单独说明
                         pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
                        //业务handler ，这里是实际处理Echo业务的Handler
                     
                        SimpleServerHandler simpleServer = new SimpleServerHandler();
                        simpleServer.DataNotity += SimpleServer_DataNotity;
                        pipeline.AddLast("db", simpleServer);
                       
                    }));

                // bootstrap绑定到指定端口的行为 就是服务端启动服务，同样的Serverbootstrap可以bind到多个端口
                if (string.IsNullOrEmpty(host))
                {
                    boundChannel = await bootstrap.BindAsync(port);
                }
                else
                {
                    boundChannel = await bootstrap.BindAsync(host,port);
                }
                IsBinded = true;


            }
            catch
            {
                //释放工作组线程
                await Task.WhenAll(
                    bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                    workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            }
        }

        private void SimpleServer_DataNotity(object sender, object msg,string flage=null)
        {
            if(DataNotify!=null)
            {
                DataNotify(sender, msg,NettySrvFlage);
            }
            else
            {
                SrvDataSource item = new SrvDataSource()
                {
                    Context = sender,
                    Message = msg,
                    Flage=NettySrvFlage
                };
                queue.Add(item);
            }
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <returns></returns>
        public SrvDataSource GetData()
        {
           return   queue.Take();
        }

        /// <summary>
        ///关闭
        /// </summary>
        /// <returns></returns>
        public async Task Close()
        {
            IsBinded = false;
            await boundChannel.CloseAsync();
            await Task.WhenAll(
                   bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)),
                   workerGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
        }

    }
}
