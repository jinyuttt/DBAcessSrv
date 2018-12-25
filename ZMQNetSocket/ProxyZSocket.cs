#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：ZMQNetSocket
* 项目描述 ：
* 类 名 称 ： ProxyZSocket
* 类 描 述 ：
* 命名空间 ：ZMQNetSocket
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




using NetMQ;
using NetMQ.Sockets;
using System.Threading.Tasks;

namespace ZMQNetSocket
{

    /* ============================================================================== 
  * 功能描述：ProxyZSocket 
  * 创 建 者：jinyu
  * 修 改 者：jinyu
  * 创建日期：2018 
  * 修改日期：2018
  * ==============================================================================*/

    public  class ProxyZSocket
    {
        private Proxy proxy = null;
        private RouterSocket frontEnd = null;
        private DealerSocket backEnd = null;

        /// <summary>
        /// 绑定本地代理
        /// </summary>
        /// <param name="frontAddress"></param>
        /// <param name="backAddress"></param>
        public void Bind(string frontAddress, string backAddress)
        {
            frontEnd = new RouterSocket();
            backEnd = new DealerSocket();
            frontEnd.Bind(frontAddress);
            backEnd.Bind(backAddress);
            proxy = new Proxy(frontEnd, backEnd);
            Task.Factory.StartNew(proxy.Start);
          
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            frontEnd.Close();
            backEnd.Close();
            proxy.Stop();
        }
    }
}
