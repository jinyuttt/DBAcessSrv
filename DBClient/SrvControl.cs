using System.Collections.Generic;
using System.Threading;

namespace DBClient
{

    /// <summary>
    /// 采用轮训获取服务端
    /// </summary>
    public class SrvControl
    {
       
      

        public static readonly SrvControl Instance = new SrvControl();

        private List<string> lstAddress = null;


        private int index = -1;

        public SrvControl()
        {
          
            lstAddress = new List<string>(10);
            lstAddress.Add("127.0.0.1:7777");//添加一个默认地址
        }

        /// <summary>
        /// 更新地址
        /// </summary>
        private void Update()
        {
            //多服务端时更新地址
            //分布式搭建准备的
        }

        /// <summary>
        /// 获取地址
        /// </summary>
        /// <returns></returns>
        public string GetCureent()
        {
            Interlocked.CompareExchange(ref index, -1, lstAddress.Count-1);
            return lstAddress[Interlocked.Increment(ref index)];
        }

        /// <summary>
        /// 所有配置地址
        /// </summary>
        public List<string> AllAddress
        {
            get { return lstAddress; }
        }
        public string Host { get; set; }

        public int Port { get; set; }
    }
}
