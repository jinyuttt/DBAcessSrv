using System;
using System.Collections.Generic;
using System.Text;

namespace DBModel
{
    /// <summary>
    /// 返回结果
    /// </summary>
   public class RequestResult
    {
       /// <summary>
       /// 返回值
       /// </summary>
        public object Result { get; set; }

       /// <summary>
       /// 结果编码
       /// </summary>
        public ErrorCode Error { get; set; }

        /// <summary>
        /// 结果编码描述
        /// </summary>
        public string ErrorMsg { get { return Error.ToDescriptionString(); } }

        /// <summary>
        /// 结果附近信息
        /// 主要是异常信息或者其它描述
        /// 例如：结果被截取
       /// </summary>
        public string ReslutMsg { get; set; }

        /// <summary>
        /// 服务端分配的一个ID
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// 客户端的ID
        /// </summary>
        public long RequestID { get; set; }
    }
}
