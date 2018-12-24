using DBModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ISQLDB
{
    public abstract class SQLAcess : ISQLConnect
    {

        public SQLAcess()
        {
           ProcessStatic obj=  ProcessStatic.instance;
        }
        public string ConnectString { get; set; }

        public virtual IDbCommand CreateCommand()
        {
            return null;
        }

        protected virtual IDbConnection GetConnectionPool()
        {
            return null;
        }

        public virtual IDataReader GetDataReader(IDbConnection connection, string sql)
        {
            return null;
        }

        public virtual IDataReader GetDataReaderWithParameter(IDbConnection connection,string sql,Dictionary<string, DBParameter> param=null)
        {
            return null;

        }


        public virtual DataSet GetSelect(string sql)
        {
            return null;
        }

        public virtual DataSet GetSelectWithParameter(string sql,Dictionary<string, DBParameter> param=null)
        {
            return null;
        }

        public virtual void InitPool()
        {
          
        }

        public  virtual IDbCommand NewCommand(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public virtual IDbCommand NewCommand(IDbConnection connection, string sql)
        {
            return null;
        }

        public virtual IDbConnection NewConnect()
        {
            return null;
        }
        public virtual IDataReader GetDataReader(string sql)
        {
            return null;
        }
        public virtual IDataReader GetDataReaderWithParameter(string sql,Dictionary<string, DBParameter> param=null)
        {
            return null;
        }

        public virtual IDbCommand NewCommand(string sql)
        {
            return null;
           
        }

        public virtual IDbCommand NewCommandWithParameter(string sql,Dictionary<string, DBParameter> param)
        {
            return null;
        }

        public virtual int ExecuteUpdate(string sql,bool scalar=false)
        {
            return 0;
        }
        public virtual int ExecuteUpdateWithParameter(string sql, bool scalar = false,Dictionary<string, DBParameter> param=null)
        {
            return 0;
        }

        public virtual IDbTransaction GetTransaction()
        {
            return null;
        }

        public virtual IDbDataParameter GetDataParameter()
        {
            return null;
        }

        public virtual IDbTransaction GetDbTransaction(IDbConnection con)

        {
           return  con.BeginTransaction();
        }

        public virtual void Close()
        {

        }

        public virtual void ClosePool()
        {

        }

        public virtual void RemoveDB()
        {

        }

        public virtual IDbDataAdapter CreateDataAdapter()
        {
            return null;
        }
    }
}
