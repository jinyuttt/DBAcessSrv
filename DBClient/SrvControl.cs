using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DBClient
{

    /// <summary>
    /// 采用轮训获取服务端
    /// </summary>
   public class SrvControl
    {
       
        /// <summary>
        /// 过滤地址
        /// </summary>
        private HashSet<string> hashAddress = null;
        public static readonly SrvControl Instance = new SrvControl();

        private List<string> lstAddress = null;


        private int index = -1;

        public SrvControl()
        {
            hashAddress = new HashSet<string>();
            lstAddress = new List<string>(10);
            lstAddress.Add("127.0.0.1:7777");//添加一个默认地址
        }
        private void Update()
        {
            //多服务端时更新地址
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
    }
}
