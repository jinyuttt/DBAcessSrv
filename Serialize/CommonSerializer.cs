using System;
using System.Collections.Generic;
using System.Text;

/**
* 命名空间: CommonClass 
* 类 名： CommonSer
* 版本 ：v1.0
* Copyright (c) year 
*/

namespace Serializer
{
    /// <summary>
    /// 功能描述    ：CommonSerializer  
    /// 创 建 者    ：jinyu
    /// 创建日期    ：2018/10/11 19:55:19 
    /// 最后修改者  ：jinyu
    /// 最后修改日期：2018/10/11 19:55:19 
    /// </summary>
    public class CommonSerializer
    {
        /// <summary>
        /// 序列化二进制
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual byte[] Serializer<T>(T obj)
        {
            return StreamSerializer.Serializer<T>(obj);
        }

        /// <summary>
        /// 反序列化二进制
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public virtual T Deserialize<T>(byte[] bytes)
        {
            return StreamSerializer.Deserialize<T>(bytes);
        }

        /// <summary>
        /// byte[]转json字符串
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public virtual string JSONBytesToString(byte[] json)
        {
            return StreamSerializer.JSONBytesToString(json);
        }

        /// <summary>
        /// json字符串转byte[]
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public virtual byte[] JSONStringToBytes(string json)
        {

            return StreamSerializer.JSONStringToBytes(json);
        }

        /// <summary>
        /// 对象转json字符串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual string JSONObjectToString<T>(T obj)
        {
            return  StreamSerializer.JSONObjectToString<T>(obj);
        }

        /// <summary>
        /// 对象直接转json的byte[]
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual byte[] JSONObjectToBytes<T>(T obj)
        {
         
            return StreamSerializer.JSONObjectToBytes<T>(obj);
        }

        /// <summary>
        /// json字符串转对象，序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public virtual T JSONStringToObject<T>(string json)
        {

            return StreamSerializer.JSONStringToObject<T>(json);
        }
    }
}
