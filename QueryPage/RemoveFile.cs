#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：RemoveFile
* 类 描 述 ：
* 命名空间 ：QueryPage
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2018
* 更新时间 ：2018
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2018. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：RemoveFile 删除缓存文件
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    /// <summary>
    /// 删除缓存文件
    /// </summary>
    public class RemoveFile
    {
        /// <summary>
        /// 缓存文件根目录
        /// </summary>
        public string CacheFileDir { get; set; }

        /// <summary>
        /// 缓存时间长度（分钟）
        /// 默认：60分钟
        /// </summary>
        public int TimeLen { get; set; }

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsRun { get; set; }

        /// <summary>
        /// 单例属性
        /// </summary>
         public static RemoveFile Instance { get { return instance; } }

        /// <summary>
        /// 单例
        /// </summary>
        private static readonly RemoveFile instance = new RemoveFile();



        /// <summary>
        /// 启动文件移除
        /// </summary>
        public void Start()
        {
            if(IsRun)
            {
                return;
            }
            IsRun = true;
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(TimeLen/2 * 60 * 1000);
                Process();
                IsRun = false;
            });
        }

        /// <summary>
        /// 处理文件移除
        /// </summary>
        /// <param name="dir"></param>
         void Process(string dir=null)
        {
            string[] all = null;
            if (dir == null)
            {
               all= Directory.GetFileSystemEntries(CacheFileDir);
            }
            else
            {
                all = Directory.GetFileSystemEntries(dir);
            }
            List<string> lstDir = new List<string>();
            foreach(string path in all)
            {
                if(File.Exists(path))
                {
                    FileInfo file = new FileInfo(path);
                    if(file.Extension!= QueryPage.CacheFileExtension)
                    {
                        continue;
                    }
                    if ((DateTime.Now-file.LastAccessTime).TotalMinutes>TimeLen)
                    {
                        //超过该时间，删除文件
                        try
                        {
                            file.Delete();
                        }
                        catch(Exception ex)
                        {

                        }
                    }
                }
                else if(Directory.Exists(path))
                {
                    lstDir.Add(path);
                }
            }
            //
            if(lstDir.Count>0)
            {
                foreach(string child in lstDir)
                {
                    Process(child);
                }
            }
        }
    }
}
