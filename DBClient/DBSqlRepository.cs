using DBModel;
using System.Collections.Generic;
using System.Data;
namespace DBClient
{

    /// <summary>
    /// SQL数据库操作
    /// </summary>
    public class DBSqlRepository : ISQLRepository
    {

        private int timeOut = 10;

        /// <summary>
        /// 超时设置
        /// 默认：10秒
        /// </summary>
        public int TimeOut { get { return timeOut; } set { timeOut = value; } }

        /// <summary>
        /// 调用的数据库
        /// 配置文件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// SQL服务类型
        /// </summary>
        public DBServerType DBServerType { get; set; }

        public DBSqlRepository()
        {
            DBServerType = DBServerType.ServerSQL;
        }

        /// <summary>
        /// 数据库执行操作
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
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

        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="modelCls"></param>
        /// <param name="modelDll"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 查询
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 分页
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 更新
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
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

        /// <summary>
       /// 更新
       /// </summary>
       /// <param name="sql"></param>
       /// <param name="parameters"></param>
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
