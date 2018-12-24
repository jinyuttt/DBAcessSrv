using System;
using System.Collections.Generic;
using System.Text;

namespace DBQuery
{

    /// <summary>
    /// 驱动信息
    /// </summary>
   public class DBConfig
    {
        private string driverDir = "Drivers";

        /// <summary>
        /// 驱动目录
        /// </summary>
        public string Drivers { get { return driverDir; } set { driverDir = value; } }

        /// <summary>
        /// 驱动提供的连接池类名称
        /// </summary>
        public string PoolClass { get; set; }

       
    }
}
