using MessagePack;
/**
* 命名空间: NStStreamCloud 
* 类 名： Stream
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace Serializer
{
    /// <summary>
    /// 功能描述    ：StreamSerializer   默认序列化
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/8 22:59:41 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/8 22:59:41 
    /// </summary>
    public static class StreamSerializer
    {
      
        private static volatile bool isInit=true;
        private static void Init()
        {
            if (isInit)
            {
                MessagePack.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
                       new[] { MessagePack.Formatters.TypelessFormatter.Instance },
                       new[] { MessagePack.Resolvers.StandardResolver.Instance });

                MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
                isInit = false;
            }

        }
            /// <summary>
        /// 序列化二进制
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] Serializer<T>(T obj)
        {
            Init();
            return  LZ4MessagePackSerializer.Serialize(obj);

        }

      
        /// <summary>
        /// 反序列化二进制
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] bytes)
        {
          
            Init();
           return LZ4MessagePackSerializer.Deserialize<T>(bytes);
        }
        

        /// <summary>
        /// byte[]转json字符串
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string JSONBytesToString(byte[]json)
        {
            Init();
            return LZ4MessagePackSerializer.ToJson(json);
        }
        
        /// <summary>
        /// 对象转json字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string  JSONObjectToString<T>(T obj)
        {
            Init();
            return LZ4MessagePackSerializer.ToJson<T>(obj);
        }

        /// <summary>
        /// 对象直接转json的byte[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[]  JSONObjectToBytes<T>(T obj )
        {
            Init();
            return LZ4MessagePackSerializer.FromJson(LZ4MessagePackSerializer.ToJson<T>(obj));
        }
        internal static byte[] JSONStringToBytes(string json)
        {
            Init();
            return LZ4MessagePackSerializer.FromJson(json);
        }

        /// <summary>
        /// json字符串转对象，序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T JSONStringToObject<T>(string json)
        {
            Init();
            return  LZ4MessagePackSerializer.Deserialize<T>(LZ4MessagePackSerializer.FromJson(json));
        }
    }
}
