using System.Collections.Generic;
using System.Data;

namespace DBClient
{

    /// <summary>
    /// SQL数据库操作接口
    /// </summary>
    public interface ISQLRepository
    {
      
        /// <summary>
        /// 操作的数据库
        /// 
        /// </summary>
         string Name { get; set; }


        /// <summary>
        /// 异步队列执行没有返回
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        void Execetue(string sql,List<Parameter> parameters = null);


        /// <summary>
        /// 查询服务端分页
        /// </summary>
        /// <typeparam name="T">返回结果</typeparam>
        /// <param name="name">查询业务名称</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="pageNum">分页号</param>
        /// <returns></returns>
        T QueryPage<T>(string name,int pageSize,int pageNum);

        /// <summary>
        /// 查询返回Model
        /// </summary>
        /// <typeparam name="T">model</typeparam>
        /// <param name="sql">SQL</param>
        /// <param name="parameters">参数</param>
        /// <param name="modelCls">model类</param>
        /// <param name="modelDll">model的DLL</param>
        /// <returns></returns>
        T Query<T>(string sql, List<Parameter> parameters=null,string modelCls=null,string modelDll=null);

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数</param>
        /// <returns></returns>
        DataTable Query(string sql, List<Parameter> parameters = null);

       /// <summary>
       /// 查询返回单值
       /// 执行Count(*)等单值返回
       /// </summary>
       /// <typeparam name="T">返回类型</typeparam>
       /// <param name="sql">SQL语句</param>
       /// <param name="parameters">参数</param>
       /// <returns></returns>
        T Query<T>(string sql, List<Parameter> parameters = null);

        /// <summary>
        /// 执行DML操作带返回
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数化</param>
        /// <returns></returns>
        T Update<T>(string sql,List<Parameter> parameters=null);

        /// <summary>
        /// 执行DML操作
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="parameters">参数</param>
        void Update(string sql,List<Parameter> parameters=null);

      


    }
}
