#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：DataSource
* 类 描 述 ：
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/11 17:12:28
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：DataSource  服务端网络接收
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class SrvDataSource
    {
        /// <summary>
        /// 服务端通信
        /// </summary>
        public object Context { get; set; }

        /// <summary>
        /// 接收数据
        /// </summary>
        public object Message { get; set; }

        /// <summary>
        /// 客户端ID
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// 服务端标识
        /// </summary>
        public string Flage { get; set; }

        private IByteBuffer initialMessage;

        public void Rsponse(byte[] rsp)
        {
            Console.WriteLine("数据回传：" + rsp.Length);
            IChannelHandlerContext ctx = Context as IChannelHandlerContext;
            if(ctx!=null)
            {
                this.initialMessage = Unpooled.Buffer(rsp.Length+8);
                ctx.WriteAndFlushAsync(initialMessage.WriteLong(ID).WriteBytes(rsp));
            }
            
        }
    }
}
