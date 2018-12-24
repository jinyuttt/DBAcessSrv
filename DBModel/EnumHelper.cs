using System;
using System.ComponentModel;
using System.Collections.Concurrent;
namespace DBModel
{
    public static class EnumHelper
    {

        private static ConcurrentDictionary<Enum, string> dicDescription = new ConcurrentDictionary<Enum, string>();

        /// <summary>
        /// 获取枚举成员的值(this是扩展方法的标志)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int ToInt(this Enum obj)
        {
            return Convert.ToInt32(obj);
        }

        /// <summary>
        /// 字符串转枚举
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this string obj) where T : struct
        {
            if (string.IsNullOrEmpty(obj))
            {
                return default(T);
            }
            try
            {
                return (T)Enum.Parse(typeof(T), obj, true);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        /// <summary>
        /// 获取指定枚举成员的描述
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string ToDescriptionString(this Enum obj)
        {
            string value = null;
            if(dicDescription.TryGetValue(obj,out value))
            {
                return value;
            }
            var attribs = (DescriptionAttribute[])obj.GetType().GetField(obj.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
            value= attribs.Length > 0 ? attribs[0].Description : obj.ToString();
            dicDescription[obj] = value;
            return value;
        }

        /// <summary>
        /// 根据枚举值，获取指定枚举类的成员描述
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string GetDescriptionString(this Type type, int? value) 
        {
           if(value.HasValue)
            {
               return ToDescriptionString((Enum)Enum.ToObject(type, value.Value));
            }
            return null;
        }

        /// <summary>
        /// 值转类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T ToEnum<T>(this int value) where T : struct
        {
            
            try
            {
                return (T)Enum.ToObject(typeof(T), value);
            }
            catch (Exception)
            {
                return default(T);
            }
        }
    }
}
