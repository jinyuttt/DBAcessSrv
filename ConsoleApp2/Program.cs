using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            var front = new RouterSocket();
            var back = new DealerSocket();
            
                front.Bind("tcp://127.0.0.1:7777");
                back.Bind("inproc://backend");

                var proxy = new Proxy(front, back);
                Task.Factory.StartNew(proxy.Start);
                Task.Factory.StartNew(()=>{
                    Thread.Sleep(1000);
                    var client = new RequestSocket("tcp://127.0.0.1:7777");
                    //client.Bind("tcp://127.0.0.1:7777");
                    client.SendFrame(Encoding.Default.GetBytes("hello"));
                    Console.WriteLine(Encoding.Default.GetString(client.ReceiveFrameBytes()));
                });
                //
                var server = new ResponseSocket();
                server.Connect("inproc://backend");
                //client.Connect("tcp://127.0.0.1:7777");
                Console.WriteLine(Encoding.Default.GetString(server.ReceiveFrameBytes()));
                server.SendFrame(Encoding.Default.GetBytes("reply"));
                //using (var client = new RequestSocket())
                //using (var server = new ResponseSocket())
                //{
                //    //client.Connect("tcp://127.0.0.1:7777");
                //    server.Connect("inproc://backend");
                //    client.Connect("tcp://127.0.0.1:7777");
                //    client.SendFrame("hello");
                //    Console.WriteLine(server.ReceiveFrameString());
                //    server.SendFrame("reply");
                //    Console.WriteLine(client.ReceiveFrameString());
                //}

                //proxy.Stop();
            
            Console.ReadLine();
        }
    }
}
