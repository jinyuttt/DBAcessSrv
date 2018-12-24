using System;
using System.Collections.Generic;
using System.Text;

/**
* 命名空间: CommonClass 
* 类 名： SerializerFactory
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace Serializer
{
    /// <summary>
    /// 功能描述    ：SerializerFactory  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/11 20:01:13 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/11 20:01:13 
    /// </summary>
   public class SerializerFactory<TSerializer> where TSerializer: CommonSerializer
    {
        private static Dictionary<string, CommonSerializer> dic_Serializer = new Dictionary<string, CommonSerializer>();
        
        //采用属性方式，不使用方法，更加舒服
        private static CommonSerializer SerializerCls
        {
            get
            {
                //不考虑锁，替换对象而已，也没有遍历
                Type serializerType = typeof(TSerializer);
                CommonSerializer serializer = null;
                if (dic_Serializer.TryGetValue(serializerType.FullName, out serializer))
                {
                    return serializer;
                }
                else
                {
                    serializer = Activator.CreateInstance<TSerializer>();
                    dic_Serializer[serializerType.FullName] = serializer;
                    return serializer;
                }
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
            return SerializerCls.Serializer<T>(obj);
        }

        /// <summary>
        /// 反序列化二进制
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] bytes)
        {
            return SerializerCls.Deserialize<T>(bytes);
        }

        /// <summary>
        /// byte[]转json字符串
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static string JSONBytesToString(byte[] json)
        {
            return SerializerCls.JSONBytesToString(json);
        }

        /// <summary>
        /// json字符串转byte[]
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static byte[] JSONStringToBytes(string json)
        {

            return SerializerCls.JSONStringToBytes(json);
        }

        /// <summary>
        /// 对象转json字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string JSONObjectToString<T>(T obj)
        {
            return SerializerCls.JSONObjectToString<T>(obj);
        }

        /// <summary>
        /// 对象直接转json的byte[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static byte[] JSONObjectToBytes<T>(T obj)
        {

            return SerializerCls.JSONObjectToBytes<T>(obj);
        }

        /// <summary>
        /// json字符串转对象，序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T JSONStringToObject<T>(string json)
        {

            return StreamSerializer.JSONStringToObject<T>(json);
        }
    }
}
