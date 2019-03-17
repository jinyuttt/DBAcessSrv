using System;

namespace NetCSDB
{
    class Program
    {
        static void Main(string[] args)
        {
            NettyDBAcessServer server = new NettyDBAcessServer();
            server.Start();
            Console.WriteLine("启动");
            Console.Read();
        }
    }
}
