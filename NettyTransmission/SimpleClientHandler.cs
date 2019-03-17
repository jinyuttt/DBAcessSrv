using System;
using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using DotNetty.Logging;
namespace NettyTransmission
{

    /// <summary>
    /// 客户端处理
    /// </summary>
    public class SimpleClientHandler : ChannelHandlerAdapter
    {
        public event NettyDataNotify DataNotify;
        private IByteBuffer initialMessage;
        private IChannelHandlerContext ctx=null;
        public bool IsConnect{ get; set; }
        public SimpleClientHandler()
        {
            this.initialMessage = Unpooled.Buffer(256);
            IsConnect = false;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            ctx.WriteAndFlushAsync(initialMessage.WriteBytes(data));
            initialMessage = Unpooled.Buffer(256);
            
        }


        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
          
            Console.WriteLine("客户端接收数据");
            if (DataNotify != null)
            {
                DataNotify(this, message);
            }
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            this.ctx = context;
            IsConnect = true;
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            //已经服务端关闭
            IsConnect = false;
            context.CloseAsync();
        }
        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }


        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            Logger.Singleton.Debug("客户端超时未接收");
            if (evt is IdleStateEvent) {
                IdleState state = ((IdleStateEvent)evt).State;
                if (state == IdleState.ReaderIdle)
                {
                    //throw new Exception("idle exception");
                    context.CloseAsync();
                }
            } else
            {
                base.UserEventTriggered(context, evt);
            }

          
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            context.CloseAsync();
        }
    }
}