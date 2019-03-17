#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NettyTransmission
* 项目描述 ：
* 类 名 称 ：NettyAddressReader
* 类 描 述 ：获取地址，服务端与
* 命名空间 ：NettyTransmission
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace NettyTransmission
{
    /* ============================================================================== 
* 功能描述：NettyAddressReader 获取地址
* 地址格式 名称=IP:Port
* 客户端与服务端不同的是服务端可以不要IP，单客户端必须要
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class NettyAddressReader
    {
        /// <summary>
        /// 读取服务端地址配置
        /// 默认空值：Path.Combine("Config", "Server.cfg")
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<NettyAddress> ReaderSrvFile(string path=null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine("Config", "Server.cfg");
            }
            List<NettyAddress> LstAddres = new List<NettyAddress>();
           if(!File.Exists(path))
            {
                return LstAddres;
            }
            using (StreamReader rd = new StreamReader(path))
            {
                string line = rd.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        //处理地址格式；默认只能是端口
                        string[] info = line.Split('=', ':');
                        NettyAddress address = new NettyAddress();
                        if (info.Length == 1)
                        {

                            address.Port = int.Parse(info[0].Trim());

                        }
                        else if (info.Length == 2)
                        {
                            //2类
                            if (line.Contains(":"))
                            {
                                //说明是IP+端口
                                address.Host = info[0].Trim();
                                address.Port = int.Parse(info[1].Trim());
                            }
                            else
                            {
                                address.Flage = info[0].Trim();
                                address.Port = int.Parse(info[1].Trim());
                            }
                        }
                        else if (info.Length == 3)
                        {
                            address.Flage = info[0].Trim();
                            address.Host = info[1].Trim();
                            address.Port = int.Parse(info[2].Trim());
                        }
                       
                        LstAddres.Add(address);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return LstAddres;
        }

        /// <summary>
        /// 获取服务端地址
        /// </summary>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static List<NettyAddress> ReaderSrv(List<string> lst)
        {

            List<NettyAddress> LstAddres = new List<NettyAddress>();
           if(lst==null||lst.Count==0)
            {
                return LstAddres;
            }
            foreach (var item in lst)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    try
                    {
                        //处理地址格式；默认只能是端口
                        string[] info = item.Split('=', ':');
                        NettyAddress address = new NettyAddress();
                        if (info.Length == 1)
                        {

                            address.Port = int.Parse(info[0].Trim());

                        }
                        else if (info.Length == 2)
                        {
                            //2类
                            if (item.Contains(":"))
                            {
                                //说明是IP+端口
                                address.Host = info[0].Trim();
                                address.Port = int.Parse(info[1].Trim());
                            }
                            else
                            {
                                address.Host = info[0].Trim();
                                address.Port = int.Parse(info[1].Trim());
                            }
                        }
                        else if (info.Length == 3)
                        {
                            address.Flage = info[0].Trim();
                            address.Host = info[1].Trim();
                            address.Port = int.Parse(info[2].Trim());
                        }

                        LstAddres.Add(address);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return LstAddres;
        }

        /// <summary>
        /// 读取服务端地址配置
        /// 默认空值：Path.Combine("Config", "Client.cfg")
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<NettyAddress> ReaderClientFile(string path = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine("Config", "Client.cfg");
            }
            List<NettyAddress> LstAddres = new List<NettyAddress>();
            if (!File.Exists(path))
            {
                return LstAddres;
            }
            using (StreamReader rd = new StreamReader(path))
            {
                string line = rd.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        //处理地址格式；默认只能是端口
                        string[] info = line.Split('=', ':');
                        NettyAddress address = new NettyAddress();
                        if (info.Length == 2)
                        {
                            //2类

                            //说明是IP+端口
                            address.Host = info[0].Trim();
                            address.Port = int.Parse(info[1].Trim());


                        }
                        else if (info.Length == 3)
                        {
                            address.Flage = info[0].Trim();
                            address.Host = info[1].Trim();
                            address.Port = int.Parse(info[2].Trim());
                        }

                        LstAddres.Add(address);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return LstAddres;
        }

        /// <summary>
        /// 获取服务端地址
        /// </summary>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static List<NettyAddress> ReaderClient(List<string> lst)
        {

            List<NettyAddress> LstAddres = new List<NettyAddress>();
            if(lst==null||lst.Count==0)
            {
                return LstAddres;
            }
            foreach (var item in lst)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    try
                    {
                        //处理地址格式；默认只能是端口
                        string[] info = item.Split('=', ':');
                        NettyAddress address = new NettyAddress();
                        if (info.Length == 2)
                        {
                            
                            //说明是IP+端口
                            address.Host = info[0].Trim();
                            address.Port = int.Parse(info[1].Trim());

                        }
                        else if (info.Length == 3)
                        {
                            address.Flage = info[0].Trim();
                            address.Host = info[1].Trim();
                            address.Port = int.Parse(info[2].Trim());
                        }

                        LstAddres.Add(address);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
            return LstAddres;
        }


    }
}
