using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DBModel
{
    /// <summary>
    /// 执行结果编码
    /// </summary>
    public enum ErrorCode
    {
       /// <summary>
       /// 成功
       /// </summary>
        [Description("执行成功")]
        Sucess,

       /// <summary>
       /// 超时
       /// </summary>
        [Description("执行超时")]
        TimeOut,

    /// <summary>
    /// 异常
    /// </summary>
        [Description("执行异常")]
        Exception,

       /// <summary>
       /// 截取
       /// </summary>
        [Description("结果被截取")]
        Truncate,
    }
}
