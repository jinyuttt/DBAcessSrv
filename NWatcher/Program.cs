using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace NWatcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Process[] processes = Process.GetProcessesByName("nexcsDB");
            foreach (Process process in processes)
            {
                string file = process.MainModule.FileName;
                if (file == Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nexcsDB.exe"))
                {
                    process.Kill();
                    Thread.Sleep(2000);
                    Process.Start(file);
                    break;
                }
            }
        }
    }
}
