using System;

namespace NetCSDB
{
    class Program
    {
        static void Main(string[] args)
        {
            DBAcessServer server = new DBAcessServer();
            server.Start();
            Console.WriteLine("启动");
            Console.Read();
        }
    }
}
