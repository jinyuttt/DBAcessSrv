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
            Process[] processes= Process.GetProcessesByName("NetCSDB");
            if(processes.Length>0)
            {
                //
                foreach(Process process in processes)
                {
                    string file = process.MainModule.FileName;
                    if(file==Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetCSDB.exe")||file == Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NetCSDB.dll"))
                    {
                        process.Kill();
                        Thread.Sleep(2000);
                        process.Start();
                        break;
                    }
                }
            }
        }
    }
}
