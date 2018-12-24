using DBClient;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            DBSqlRepository repository = new DBSqlRepository();
            while (true)
            {
                repository.Query("select * from Test");
                Console.WriteLine("Hello World!");
                Console.ReadLine();
            }

        }
    }
}
