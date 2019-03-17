

namespace NetCSDB
{

    /// <summary>
    /// 登录验证配置
    /// </summary>
    internal class SrvSetting
    {
        /// <summary>
        /// 是否允许默认验证
        /// </summary>
        public static bool IsAuthorization { get;  set; }

        /// <summary>
        /// 是否有文件授权验证E:\study\DBAcessSrv\DBAcessSrv\NetCSDB\SrvSetting.cs
        /// </summary>
        public static bool IsFileauthorization { get;  set; }

        /// <summary>
        /// 验证文件路径
        /// </summary>
        public static string AuthorizationFile { get;  set; }
    }
}