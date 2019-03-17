using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Factory.StartNew(() =>
            {
                var cts = new CancellationTokenSource(5000);

                //超时处理
                var cancell = cts.Token.Register(() =>
                {

                    Console.WriteLine("超时");

                });
                var taskResult = Task.Factory.StartNew(() =>
                 {
                     //  return "Test";
                     Console.WriteLine("执行任务");
                     Thread.Sleep(8000);
                     return "Test";

                 }, cts.Token);

                long id = taskResult.Id;

                var result = taskResult.Result;
                cancell.Dispose();

                Console.WriteLine(result);
            });
            Console.ReadKey();
        }
    }
}
