using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DBClient
{
   public interface ISQLRepository
    {
      
         string Name { get; set; }


        /// <summary>
        /// 异步队列执行没有返回
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        void Execetue(string sql,List<Parameter> parameters = null);



        T QueryPage<T>(string name,int pageSize,int pageNum);

        T Query<T>(string sql, List<Parameter> parameters=null,string modelCls=null,string modelDll=null);

        DataTable Query(string sql, List<Parameter> parameters = null);

        T Query<T>(string sql, List<Parameter> parameters = null);

        T Update<T>(string sql,List<Parameter> parameters=null);

        void Update(string sql,List<Parameter> parameters=null);

      


    }
}
