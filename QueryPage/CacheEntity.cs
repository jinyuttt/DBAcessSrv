﻿#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：CacheEntity
* 类 描 述 ：
* 命名空间 ：QueryPage
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


using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：CacheEntity 缓存实体
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

       /// <summary>
       /// 缓存实体
       /// </summary>
   public class CacheEntity
    {
        /// <summary>
        /// MODEL
        /// </summary>
        public List<object> Model { get; set; }

        /// <summary>
        /// DataTable
        /// </summary>
        public DataTable DataTable { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 对应的页码
        /// </summary>
        public int PageNum { get; set; }

        public CacheEntity(List<object> list,int pageNum)
        {
            DateTime = DateTime.Now;
            Model = list;
            this.PageNum = pageNum;

        }
        public CacheEntity(DataTable dt,int pageNum)
        {
            DateTime = DateTime.Now;
            DataTable = dt;
            this.PageNum = pageNum;

        }
    }
}
