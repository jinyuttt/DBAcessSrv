#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：ZMQNetSocket
* 项目描述 ：
* 类 名 称 ：ZMQClient
* 类 描 述 ：
* 命名空间 ：ZMQNetSocket
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2018
* 更新时间 ：2018
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2018. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using NetMQ.Sockets;
using NetMQ;
using System;

namespace ZMQNetSocket
{
    /* ============================================================================== 
    * 功能描述：ZMQClient 
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    public class ZMQClient
    {
        RequestSocket socket = null;

        public string Address { get; set; }

        public void Send(string host, int port,byte[] buf)
        {
            using (var client = new RequestSocket())  // connect
            {
                client.Connect("tcp://" + host + ":" + port);
                client.SendFrame(buf);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="buf"></param>
        /// <returns></returns>
        public byte[] Send(string address, byte[] buf)
        {
          
            using (var client = new RequestSocket("tcp://" + address))  // connect
            {
                       client.SendFrame(buf);
                return client.ReceiveFrameBytes();
            }
        }

        /// <summary>
        /// 长连接发送
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public byte[] Send(byte[] buf)
        {
            if(socket==null)
            {
                socket = new RequestSocket("tcp://" + Address);
            }
            socket.SendFrame(buf);
            return socket.ReceiveFrameBytes();
        }

        /// <summary>
        /// 关闭长连接
        /// </summary>
        public void Close()
        {
            if (socket != null)
            {
                socket.Close();
            }
        }
    }
}
