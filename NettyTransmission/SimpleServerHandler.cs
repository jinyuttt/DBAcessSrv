#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：SimpleServerHandler
* 类 描 述 ：
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 3:47:00
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;
using System;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：SimpleServerHandler 
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
   
    public class SimpleServerHandler: ChannelHandlerAdapter
    {
    
        public event NettyDataNotify DataNotity;
     
       
        public SimpleServerHandler()
        {
         
        }

     


        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
          
            Console.WriteLine("SimpleServerHandler读取数据");
            //var byteBuffer = message as IByteBuffer;
            //byte[] rev = new byte[byteBuffer.ReadableBytes];
            //byteBuffer.ReadBytes(rev);
            ////做一些接收处理
            if (DataNotity != null)
            {
                Console.WriteLine("SimpleServerHandler推送数据");
                DataNotity(context, message);
            }

        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Console.WriteLine("客户端连接");
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }
        public override void ExceptionCaught(IChannelHandlerContext context, Exception e)
        {
            Console.WriteLine("{0}", e.Message);
            context.CloseAsync();
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            Console.WriteLine("服务端超时");
            if (evt is IdleStateEvent)
            {
                var eventState = evt as IdleStateEvent;

                if (eventState != null)
                {

                    if (eventState.State == IdleState.ReaderIdle)
                    {
                        context.CloseAsync();
                    }
                    else
                    {
                        context.WriteAndFlushAsync("heart");
                    }
                }
            }
        }
    }
}
