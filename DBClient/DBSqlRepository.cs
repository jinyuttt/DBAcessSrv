using DBModel;
using System.Collections.Generic;
using System.Data;
namespace DBClient
{

    /// <summary>
    /// 数据库操作
    /// </summary>
    public class DBSqlRepository : ISQLRepository
    {

        private int timeOut = 10;

        public int TimeOut { get { return timeOut; } set { timeOut = value; } }
        public string Name { get; set; }

        /// <summary>
        /// SQL服务类型
        /// </summary>
        public DBServerType DBServerType { get; set; }

        public DBSqlRepository()
        {
            DBServerType = DBServerType.ServerSQL;
        }

        public void Execetue(string sql, List<Parameter> parameters = null)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = false,
                IsJson = false,
                IsModel = false,
                IsPage = true,
                IsScala = true,
                ModelCls = null,
                ModelDLL = null,
                Paramter = null,
                SQL = null,
                SQLParamter = null,
                TimeOut = timeOut,
                PageInfo = null
            };
            if (parameters != null)
            {
                transfer.SQLParamter = new Dictionary<string, DBParameter>();
                foreach (var item in parameters)
                {
                    transfer.SQLParamter[item.Name] = new DBParameter()
                    {
                        Value = item.Value,
                        DbType = item.DbType.ToInt(),
                        ParameterDirection = item.ParamDirection.ToInt()
                    };
                }
            }
            DataStream.Instance.Push(transfer);
        }

        public T Query<T>(string sql, List<Parameter> parameters = null, string modelCls = null, string modelDll = null)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = true,
                IsJson = false,
                IsModel = true,
                IsPage = false,
                IsScala = false,
                ModelCls = modelCls,
                ModelDLL = modelDll,
                Paramter = null,
                SQLParamter = null,
                SQL = sql,
                TimeOut = timeOut,
                PageInfo = null
            };
            if(parameters!=null)
            {
                transfer.SQLParamter = new Dictionary<string, DBParameter>();
                foreach (var item in parameters)
                {
                    transfer.SQLParamter[item.Name] = new DBParameter()
                    {
                        Value = item.Value,
                        DbType = item.DbType.ToInt(),
                        ParameterDirection = item.ParamDirection.ToInt()
                    };
                }
            }
            if (typeof(T)==typeof(string))
            {
                transfer.IsJson = true;
            }
          return DataStream.Instance.Send<T>(transfer);
        }

        public DataTable Query(string sql, List<Parameter> parameters = null)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = true,
                IsJson = false,
                IsModel = false,
                IsPage = false,
                IsScala = false,
                ModelCls = null,
                ModelDLL = null,
                Paramter = null,
                SQL = sql,
                SQLParamter=null,
                TimeOut = timeOut,
                PageInfo = null
            };
            if (parameters != null)
            {
                transfer.SQLParamter = new Dictionary<string, DBParameter>();
                foreach (var item in parameters)
                {
                    transfer.SQLParamter[item.Name] = new DBParameter()
                    {
                        Value = item.Value,
                        DbType = item.DbType.ToInt(),
                        ParameterDirection = item.ParamDirection.ToInt()
                    };
                }
            }
            return DataStream.Instance.Send<DataTable>(transfer);
        }

        public T Query<T>(string sql, List<Parameter> parameters = null)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = true,
                IsJson = false,
                IsModel = false,
                IsPage = false,
                IsScala = true,
                ModelCls = null,
                ModelDLL = null,
                Paramter = null,
                SQL = sql,
                SQLParamter=null,
                TimeOut = timeOut,
                PageInfo = null
            };
            if (parameters != null)
            {
                transfer.SQLParamter = new Dictionary<string, DBParameter>();
                foreach (var item in parameters)
                {
                    transfer.SQLParamter[item.Name] = new DBParameter()
                    {
                        Value = item.Value,
                        DbType = item.DbType.ToInt(),
                        ParameterDirection = item.ParamDirection.ToInt()
                    };
                }
            }
            return DataStream.Instance.Send<T>(transfer);
        }

        public T QueryPage<T>(string name, int pageSize, int pageNum)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = true,
                IsJson = false,
                IsModel = false,
                IsPage = true,
                IsScala = true,
                ModelCls = null,
                ModelDLL = null,
                Paramter = null,
                SQL = null,
                SQLParamter=null,
                TimeOut = timeOut,
                PageInfo = new QueryPageInfo()
                {
                    IsCache = false,
                    PageNum = pageNum,
                    QueryName = name
                }
            };
            return DataStream.Instance.Send<T>(transfer);
        }

        public T Update<T>(string sql, List<Parameter> parameters = null)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = false,
                IsJson = false,
                IsModel = false,
                IsPage = true,
                IsScala = true,
                ModelCls = null,
                ModelDLL = null,
                Paramter = null,
                SQL = null,
                SQLParamter = null,
                TimeOut = timeOut,
                PageInfo = null
            };
            if (parameters != null)
            {
                transfer.SQLParamter = new Dictionary<string, DBParameter>();
                foreach (var item in parameters)
                {
                    transfer.SQLParamter[item.Name] = new DBParameter()
                    {
                        Value = item.Value,
                        DbType = item.DbType.ToInt(),
                        ParameterDirection = item.ParamDirection.ToInt()
                    };
                }
            }
            return DataStream.Instance.Send<T>(transfer);
        }

        public void Update(string sql, List<Parameter> parameters = null)
        {
            DBTransfer transfer = new DBTransfer
            {
                DBCfg = Name,
                DBServerType = DBServerType,
                IsQuery = false,
                IsJson = false,
                IsModel = false,
                IsPage = true,
                IsScala = true,
                ModelCls = null,
                ModelDLL = null,
                Paramter = null,
                SQL = null,
                SQLParamter = null,
                TimeOut = timeOut,
                PageInfo = null
            };
            if (parameters != null)
            {
                transfer.SQLParamter = new Dictionary<string, DBParameter>();
                foreach (var item in parameters)
                {
                    transfer.SQLParamter[item.Name] = new DBParameter()
                    {
                        Value = item.Value,
                        DbType = item.DbType.ToInt(),
                        ParameterDirection = item.ParamDirection.ToInt()
                    };
                }
            }
             DataStream.Instance.Send(transfer);
        }

    }
}
