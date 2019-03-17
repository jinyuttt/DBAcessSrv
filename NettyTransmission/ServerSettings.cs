namespace NettyTransmission
{
    public class ServerSettings
    {
        public static bool IsSsl { get;  set; }
       
        public static bool UseLibuv { get; internal set; }
    }
}