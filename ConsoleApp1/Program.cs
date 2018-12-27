using DBClient;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
           ISQLRepository repository = new DBSqlRepository();
            while (true)
            {
                try
                {
                    repository.Query("select * from \"Test\"");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                Console.WriteLine("Hello World!");
                Console.ReadLine();
            }
            //while (true)
            //{
            //    var client = new RequestSocket("tcp://127.0.0.1:7777");
            //    client.SendFrame(System.Text.Encoding.Default.GetBytes("Hello"));
            //    byte[] m2 = client.ReceiveFrameBytes();
            //    Console.WriteLine("From Server: {0}", Encoding.Default.GetString(m2));
            //    Console.ReadLine();
            //}

        }
    }
}
