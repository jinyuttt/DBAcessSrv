using NettyTransmission;
using ZMQNetSocket;
namespace DBClient
{
    /// <summary>
    /// 
    /// </summary>
    public  class RequestServer
    {
        private string address = "127.0.0.1:7777";
        private ZMQClient mqClient = null;
       
       
        public string Address { get { return address; } set { address = value; } }

        internal byte[] Request(byte[] req)
        {
             ZMQClient client = new ZMQClient();
             return   client.Send(address, req);
        }

       
        internal byte[] KeepRequest(byte[] req)
        {
            if (mqClient == null)
            {
                mqClient = new ZMQClient();
                mqClient.Address = address;
            }
            return mqClient.Send(req);
        }


        internal void  KeepClose()
        {
            mqClient.Address = address;
            mqClient.Close();
            mqClient = null;
        }
    }
}
