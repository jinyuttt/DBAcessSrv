using NetMQ;
using NetMQ.Sockets;

namespace ZMQNetSocket
{
    public class TCPUserToken
    {
        public byte[] Data { get; set; }

        public ResponseSocket Socket { get; set; }

        private NetMQ.Msg msg;

        public void Rsp(byte[] buffer)
        {
          
            Socket.SendFrame(buffer);
         //   msg.Put(buffer, offset, len);
            //Socket.TrySend(ref msg, new System.TimeSpan(0, 0, 10), false);
        }
        public void Rsp()
        {
           
            msg.Put(Data, 0, Data.Length);
            Socket.TrySend(ref msg, new System.TimeSpan(0, 0, 10), false);
        }
    }
}
