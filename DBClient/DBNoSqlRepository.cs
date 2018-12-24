#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：DBClient
* 项目描述 ：
* 类 名 称 ： DBNoSqlRepository
* 类 描 述 ：
* 命名空间 ：DBClient
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
using System.Text;

namespace DBClient
{

    /* ============================================================================== 
  * 功能描述：DBNoSqlRepository 
  * 创 建 者：jinyu
  * 修 改 者：jinyu
  * 创建日期：2018 
  * 修改日期：2018
  * ==============================================================================*/

    public class DBNoSqlRepository : IKVRepository
    {

        /// <summary>
        /// NOSQL项，LocalKV,NoSQL
        /// </summary>
        public DBServerType DBServerType { get; set; }

        public DBNoSqlRepository()
        {
            DBServerType = DBServerType.LocalKV;
        }
            public void Clear()
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Clear",
                DBServerType = this.DBServerType
            };
           
             DataStream.Instance.Send(transfer);
        }

        public void Delete<T>(T key)
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Delete",
                DBServerType = this.DBServerType,
                Paramter = new Dictionary<object, object>(),
          
            };
            transfer.Paramter.Add(key, null);
            DataStream.Instance.Send(transfer);
        }

        public void Delete<T>(List<T> list)
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Delete",
                DBServerType = this.DBServerType,
                Paramter = new Dictionary<object, object>(),

            };
            foreach (T key in list)
            {
                transfer.Paramter.Add(key, null);
            }
            DataStream.Instance.Send(transfer);
        }

        public TValue GetValue<TKey, TValue>(TKey key)
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Get",
                DBServerType = this.DBServerType,
                Paramter = new Dictionary<object, object>(),

            };
           transfer.Paramter.Add(key, null);
            
           return DataStream.Instance.Send<TValue>(transfer);
        }

        public Dictionary<TKey, TValue> GetValue<TKey, TValue>(List<TKey> lst)
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Get",
                DBServerType = this.DBServerType,
                Paramter = new Dictionary<object, object>(),

            };

            foreach (TKey key in lst)
            {
                transfer.Paramter.Add(key, null);
            }

            return DataStream.Instance.Send<Dictionary<TKey, TValue>>(transfer);
        }

        public void Put<TKey, TValue>(TKey key, TValue value)
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Put",
                DBServerType = this.DBServerType,
                Paramter = new Dictionary<object, object>(),

            };
            transfer.Paramter.Add(key, value);
            DataStream.Instance.Send(transfer);
        }

        public void Put<TKey, TValue>(Dictionary<TKey, TValue> kv)
        {
            DBTransfer transfer = new DBTransfer()
            {
                SQL = "Put",
                DBServerType = this.DBServerType,
                Paramter = new Dictionary<object, object>(),

            };
            foreach(KeyValuePair<TKey,TValue> item in kv)
            {
                transfer.Paramter[item.Key] = item.Value;
            }
            DataStream.Instance.Send(transfer);
        }
    }
}
