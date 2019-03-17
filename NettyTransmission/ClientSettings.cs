namespace NettyTransmission
{
    internal class ClientSettings
    {
        public static bool IsSsl { get; internal set; }
      
        /// <summary>
        /// 是否启用连接超时
        /// </summary>
        public static bool IsConnectTimeout { get; set; }

        /// <summary>
        /// 网络不能提交时，内存中缓存的包数
        /// 默认：1000
        /// </summary>
        public static int MemoryCacheNum { get; set; }

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        public static int TimeOut { get; set; }

        /// <summary>
        /// 是否自动存储文件
        /// 默认：false
        /// </summary>
        public static bool IsSave { get; set; }

        /// <summary>
        /// 每个文件大小(单位:M)
        /// 默认：10M
        /// </summary>
        public static int FileSize { get; set; }

        /// <summary>
        /// 网络恢复是否自动重发
        /// 默认：false
        /// </summary>
        public static  bool AutoReSend { get; set; }

        /// <summary>
        /// 限制存储数据量(文件个数)
        /// 默认：0,无限存储直到磁盘满异常
        /// </summary>
        public static int LimitFileNum { get; set; }

        /// <summary>
        /// 文件存储目录
        /// 默认:Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClientFile")
        /// </summary>
        public static string DirPath { get; set; }
    }
}