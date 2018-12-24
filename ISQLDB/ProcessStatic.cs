using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ISQLDB
{
   public class ProcessStatic
    {
        public readonly static ProcessStatic instance = new ProcessStatic();
        public ProcessStatic()
        {
            //
            try
            {
                String pwd = Environment.CurrentDirectory;
                //pwd = Path.Combine(pwd, "..");
                // pwd = Path.Combine(pwd, "..");
                if (IntPtr.Size == 4)
                    pwd = Path.Combine(pwd, "Win32");
                else
                    pwd = Path.Combine(pwd, "x64");
                //#if DEBUG
                //                pwd = Path.Combine(pwd, "Debug");
                //#else
                //                pwd = Path.Combine(pwd, "Release");
                //#endif
                pwd += ";" + Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", pwd);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unable to set the PATH environment variable.");
                Console.WriteLine(e.Message);
              
            }
        }
    }
}
