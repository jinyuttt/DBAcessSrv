using DBModel;
using System;
using System.Collections.Generic;
using System.Text;
using Serializer;
using ZMQNetSocket;
namespace DBClient
{
    /// <summary>
    /// 
    /// </summary>
  public  class RequestServer
    {
        private string address = "127.0.0.1:7777";
        private ZMQClient mqCliet = null;
        public string Address { get { return address; } set { address = value; } }

        internal byte[] Request(byte[] req)
        {
          
            ZMQClient client = new ZMQClient();
             return client.Send(address, req);
        }
        internal byte[] KeepRequest(byte[] req)
        {
            if (mqCliet == null)
            {
                mqCliet = new ZMQClient();
                mqCliet.Address = address;
            }
            return mqCliet.Send(req);
        }


        internal void  KeepClose()
        {

            mqCliet.Address = address;
            mqCliet.Close();
            mqCliet = null;
        }
    }
}
