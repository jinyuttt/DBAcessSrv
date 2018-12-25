using NetMQ;
using NetMQ.Sockets;
using System;

namespace ZMQNetSocket
{
    public class TCPUserToken
    {
        internal TCPUserToken(ZMQ_UserToken_Pool pool)
        {
            Token_Pool = pool;
            AcessTime = DateTime.Now;
        }
        public TCPUserToken()
        {

        }
        public byte[] Data { get; set; }

        public ResponseSocket Socket { get; set; }

        private ZMQ_UserToken_Pool Token_Pool { get; set; }

        internal DateTime AcessTime { get; set; }

        internal int Use = 1;

        public void Rsp(byte[] buffer)
        {
            Socket.SendFrame(buffer);
            if (Token_Pool != null)
            {
                Token_Pool.Push(this);
            }
        }
        public void Rsp()
        {
            Socket.SendFrame(Data);
            if(Token_Pool!=null)
            {
                Token_Pool.Push(this);
            }
        }
    }
}
