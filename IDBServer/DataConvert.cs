#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DBServer
* 项目描述 ：
* 类 名 称 ：DataConvert
* 类 描 述 ：
* 命名空间 ：DBServer
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


using DBModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace DBServer
{
    /* ============================================================================== 
    * 功能描述：DataConvert  datatable转model
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

   public class DataConvert<T> where T : new()
    {
        /// <summary>
        /// 将DataRow行转换成Entity
        /// </summary>
        /// <param name="dr"></param>
        /// <returns></returns>
        public static T ToEntity(DataRow dr)
        {
            T entity = new T();
            Type info = typeof(T);
            var members = info.GetMembers();
            foreach (var mi in members)
            {
                if (mi.MemberType == MemberTypes.Property)
                {
                    //读取属性上的DataField特性
                    object[] attributes = mi.GetCustomAttributes(typeof(DataFieldAttribute), true);
                    foreach (var attr in attributes)
                    {
                        var dataFieldAttr = attr as DataFieldAttribute;
                        if (dataFieldAttr != null)
                        {
                            var propInfo = info.GetProperty(mi.Name);
                            if (dr.Table.Columns.Contains(dataFieldAttr.ColumnName))
                            {
                                //根据ColumnName，将dr中的相对字段赋值给Entity属性
                                propInfo.SetValue(entity,
                                                  Convert.ChangeType(dr[dataFieldAttr.ColumnName], propInfo.PropertyType),
                                                  null);
                            }

                        }
                    }
                }
            }
            return entity;
        }

        /// <summary>
        /// 将DataTable转换成Entity列表
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static List<T> ToList(DataTable dt)
        {
            List<T> list = new List<T>(dt.Rows.Count);
            foreach (DataRow dr in dt.Rows)
            {
                list.Add(ToEntity(dr));
            }
            return list;
        }
    }
}
