#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：ClientCacheFile
* 类 描 述 ：
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/17 13:23:20
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：ClientCacheFile  存储数据
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class ClientCacheFile
    {
        
        private const int MSize = 1024 * 1024;
        //格式 YYYYDDMMHHmmss;
        private const string zzbds = "((([0-9]{3}[1-9]|[0-9]{2}[1-9][0-9]{1}|[0-9]{1}[1-9][0-9]{2}|[1-9][0-9]{3})(((0[13578]|1[02])(0[1-9]|[12][0-9]|3[01]))|" +
   "((0[469] | 11)(0[1 - 9] |[12][0 - 9] | 30)) | (02(0[1 - 9] |[1][0 - 9] | 2[0 - 8]))))|((([0 - 9]{2})(0[48]|[2468] [048]|[13579] [26])|" +
"((0[48]|[2468] [048]|[3579] [26])00))0229))" +
"([0 - 1][0 - 9]|2[0-3])([0 - 5][0 - 9]) ([0 - 5][0 - 9])$";
        private static object wlock_obj = new object();
        private static object rlock_obj = new object();

        /// <summary>
        /// 分类存储
        /// </summary>
        /// <param name="client"></param>
        public static void Write(ClientData client)
        {
            string Dir = ClientSettings.DirPath;
            if(string.IsNullOrEmpty(Dir))
            {
                Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClientFile");
            }
            string subDir= Path.Combine(Dir, client.ClientFlage);
            if (!Directory.Exists(subDir))
            {
                Directory.CreateDirectory(subDir);
            }
            //
            string lastFile = null;
            lock (wlock_obj)
            {
               
                    string[] files = Directory.GetFiles(subDir, zzbds + ".dat");
                    if (files.Length == 0)
                    {
                        lastFile = Path.Combine(subDir, DateTime.Now.ToString("YYYYMMDDHHmmss") + ".dat");
                    }
                    else
                    {
                        lastFile = files.Max();
                    }
                
                FileInfo info = new FileInfo(lastFile);
                if (info.Length > ClientSettings.FileSize * MSize)
                {
                    lastFile = Path.Combine(Dir, DateTime.Now.ToString("YYYYMMDDHHmmss") + ".dat");
                    //换文件了
                    DeleteFile(subDir);
                }
               
                //
                byte[] Len = BitConverter.GetBytes(client.Data.Length);
                using (FileStream fileStream = new FileStream(lastFile, FileMode.Append, FileAccess.ReadWrite))
                {
                    fileStream.Write(Len, 0, Len.Length);
                    fileStream.Write(client.Data, 0, client.Data.Length);
                }
            }

        }


        /// <summary>
        /// 顺序读取文件
        /// </summary>
        /// <returns></returns>
        public static List<ClientData> Read(string clientFlage)
        {
          
            //
            List<ClientData> lst = new List<ClientData>();
            string Dir = ClientSettings.DirPath;
            if (string.IsNullOrEmpty(Dir))
            {
                Dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClientFile");
            }
            string subDir = Path.Combine(Dir, clientFlage);
            if(!Directory.Exists(subDir))
            {
                return lst;
            }
            lock (rlock_obj)
            {
               
                string[] files = Directory.GetFiles(subDir, zzbds + ".dat");
                if (files.Length == 0)
                {
                    return lst;
                }
                string f = files.Min();
                byte[] Len = new byte[4];
                int len = 0;
                byte[] data = null;
                using (FileStream fileStream = new FileStream(f, FileMode.Open, FileAccess.ReadWrite))
                {
                    while (fileStream.Position < fileStream.Length)
                    {
                        ClientData client;
                        fileStream.Read(Len, 0, 4);
                        len = BitConverter.ToInt32(Len, 0);//名称总长+数据总长，没有算4字节长度
                        data = new byte[len];
                        fileStream.Read(data, 0, data.Length);
                        client.ClientFlage = clientFlage;
                        client.Data = data;
                        lst.Add(client);
                    }
                }
                File.Delete(f);
            }
            return lst;
        }
      
        private static void DeleteFile(string dir)
        {
           if(ClientSettings.LimitFileNum<=0)
            {
                return;
            }
           
            Task.Factory.StartNew(() =>
            {
                string[] files = Directory.GetFiles(dir, zzbds + ".dat");
                if (files.Length == 0)
                {
                    return ;
                }
                if(files.Length>ClientSettings.LimitFileNum)
                {
                    var lst=files.ToList();
                    lst.Sort();
                    int num = lst.Count - ClientSettings.LimitFileNum;
                    for(int i=0;i<num;i++)
                    {
                        File.Delete(lst[i]);
                    }
                  
                }
            });
        }
    }
}
