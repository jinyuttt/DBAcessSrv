using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nexcs
{
    class Program
    {
        static void Main(string[] args)
        {
            DBQueryServer server = new DBQueryServer();
            server.Start();
            Console.Read();
        }
    }
}
