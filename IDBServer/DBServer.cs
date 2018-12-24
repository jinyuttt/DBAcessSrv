using BDB;
using DBModel;
using DBSqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.IO;
using RedisClient;
using QueryPage;
using System.Text.RegularExpressions;
using SQLDB;

namespace DBServer
{
    public class DBAcessSrv
    {
        private BDBHelper<object,object> bDBHelper = null;
        private SqliteHelper sqliteHelper = null;
        private SqliteHelper memHelper = null;
        private Dictionary<string, object> dicModels = null;
        private LocalCache<object, object> localCache = null;
        private QueryPage.QueryPage queryPage = null;
        #region 默认值
            string kvdir = "";
            
                  #endregion
        /// <summary>
        /// 本地SQL数据库文件
        /// 默认LocalSQLDB/localdb.db
        /// </summary>
        public string LocalSQLFile { get; set; }

        /// <summary>
        /// 本地KV根目录
        /// 默认默认LocalKVDB
        /// </summary>
        public string KVDir { get; set; }

        /// <summary>
        /// SQL数据库配置文件
        /// 默认DBCfg
        /// </summary>
        public string SQLCfgDir { get; set; }

        /// <summary>
        /// Redis地址配置（包括目录）
        /// 默认DBCfg\Redis.Cfg
        /// </summary>
        public string RedisCfg { get; set; }

        public DBAcessSrv()
        {
            if(!Directory.Exists("LocalSQLDB"))
            {
                Directory.CreateDirectory("LocalSQLDB");
            }
            if (!Directory.Exists("DBCfg"))
            {
                Directory.CreateDirectory("DBCfg");
            }
            LocalSQLFile = Path.Combine("LocalSQLDB", "localdb.db");
            KVDir = "LocalKVDB";
            SQLCfgDir = "DBCfg";
            RedisCfg = Path.Combine("DBCfg", "Redis.Cfg");
            memHelper = new SqliteHelper();
            bDBHelper = new BDBHelper<object, object>();
            
            kvdir = bDBHelper.EnvHome;
            bDBHelper.EnvHome = KVDir;
            bDBHelper.Reset();
            sqliteHelper = new SqliteHelper();
            sqliteHelper.ConnectString = LocalSQLFile;
            sqliteHelper.CreateEmptyDB(LocalSQLFile);
          //  DBAcessPool.SetConfigDir(SQLCfgDir);
            LoadRedisCfg();

        }

        /// <summary>
        /// 不使用默认值时重新初始化
        /// </summary>

        public void ResetInit()
        {
            RemoveConfig();
            bDBHelper.EnvHome = KVDir;
            bDBHelper.Reset();
            sqliteHelper.ConnectString = LocalSQLFile;
            sqliteHelper.CreateEmptyDB(LocalSQLFile);
            //DBAcessPool.SetConfigDir(SQLCfgDir);
            LoadRedisCfg();
        }


        /// <summary>
        /// 移除默认配置
        /// </summary>
        private void RemoveConfig()
        {
            //移除KV配置
            if(Directory.Exists(kvdir))
            {
                Directory.Delete(kvdir, true);
            }
            //KV默认
            if (Directory.Exists("LocalKVDB"))
            {
                Directory.Delete("LocalKVDB", true);
            }
            //
            if (Directory.Exists("LocalSQLDB"))
            {
                Directory.Delete("LocalSQLDB", true);
            }
        }

        private void  LoadRedisCfg()
        {
           
            if(!File.Exists(RedisCfg))
            {
                return;
            }
            Redis.Address = new List<string>();
            using (StreamReader rd = new StreamReader(RedisCfg))
            {
                while(rd.Peek()!=-1)
                {
                    Redis.Address.Add(rd.ReadLine().Trim());
                }
            }
        }


        private void DeleteDirFile(string dir)
        {
            string[] files = Directory.GetFiles(dir);
            if(files.Length>0)
            {
                foreach(string file in files)
                {
                    File.Delete(file);
                }
            }
             string[] dirs=  Directory.GetDirectories(dir);
            if(dirs.Length>0)
            {
              foreach(string child in dirs)
                {
                    DeleteDirFile(child);
                }
            }
            else
            {
                Directory.Delete(dir);
            }
        }



        public RequestResult Execete(long rspid,DBTransfer transfer)
        {
            RequestResult result = null;
            if(!string.IsNullOrEmpty(transfer.SQL))
            {
                transfer.SQL= Regex.Replace(transfer.SQL, "\\s{2,}", ",");
            }
            switch (transfer.DBServerType)
            {
                case DBServerType.LocalKV:
                    result= KV(transfer);
                    break;
                case DBServerType.LocalSQL:
                    result = LocalSQL(transfer);
                    break;
                case DBServerType.MemoryNoSQL:
                    result = NoSQL(transfer);
                    break;
                case DBServerType.MemorySQL:
                    result = MemorySQL(transfer);
                    break;
                case DBServerType.ServerSQL:
                    result = ServerSQL(transfer);
                   // result = ServerSQLWithPage(transfer);
                    break;
                case DBServerType.NoSQL:
                    result = NoSQL(transfer);
                    break;

            }
            result.RequestID = transfer.RequestID;
            result.ID = rspid;
            return result;
        }

        private RequestResult ServerSQL(DBTransfer transfer)
        {
            RequestResult result = new RequestResult();
            result.Error = ErrorCode.Sucess;
             DBAcessPool dBAcess = new DBAcessPool();
            dBAcess.DBName = transfer.DBCfg;
            if (transfer.IsQuery)
            {
                try
                {
                    DataSet ds = dBAcess.GetSelectWithParameter(transfer.SQL, transfer.SQLParamter);
                    if(ds.Tables.Count>0)
                    {
                       DataTable dt= ds.Tables[0];
                        result.Result = dt;
                        if(transfer.IsModel&&!string.IsNullOrEmpty(transfer.ModelCls))
                        {
                            object value = null;
                            if(!dicModels.TryGetValue(transfer.ModelCls,out value))
                            {

                                string path = null;
                                 path= transfer.ModelDLL;
                                if(string.IsNullOrEmpty(path))
                                {
                                   path= transfer.ModelCls.Substring(0, transfer.ModelCls.LastIndexOf("."));
                                }
                                 Assembly assembly=  Assembly.LoadFrom(Path.Combine("Models", path, ".dll"));
                                 value= assembly.CreateInstance(transfer.ModelCls,true);
                            }
                            //
                            try
                            {
                                result.Result = DataConvert<object>.ToList(dt);
                            }
                            catch(Exception ex)
                            {
                                result.Error = ErrorCode.Exception;
                                result.ReslutMsg = "转换Model错误，" + ex.Message;
                            }
                        }
                        
                    }
                    
                }
                catch(Exception ex)
                {
                    result.Error = ErrorCode.Exception;
                    result.ReslutMsg ="查询错误，"+ ex.Message;
                }
            }
            else
            {
                try
                {
                    int r = dBAcess.ExecuteUpdateWithParameter(transfer.SQL, transfer.IsScala, transfer.SQLParamter);
                    result.Result = r;
                }
                catch(Exception ex)
                {
                    result.Error = ErrorCode.Exception;
                    result.ReslutMsg = "执行错误，" + ex.Message;
                }

            }
            dBAcess.Close();
            return result;
        }

        private object QueryPage(DBTransfer transfer)
        {
            if(!transfer.IsPage)
            {
                return null;
            }
            if(queryPage==null)
            {
                queryPage = new QueryPage.QueryPage();
                queryPage.PageConfig = new QueryPageConfig();
                queryPage.PageConfig.LoadConfig();//读取默认配置；
                queryPage.Init();
            }
           
                if (transfer.IsModel)
                {
                    return queryPage.GetListModel(transfer.DBCfg, transfer.PageInfo.QueryName, transfer.PageInfo.PageNum);
                }
                else
                {
                    return queryPage.GetDataTable(transfer.DBCfg, transfer.PageInfo.QueryName, transfer.PageInfo.PageNum);
                }
            
        }
         

        /// <summary>
        /// 带分页缓存功能
        /// 只给一个例子
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private RequestResult ServerSQLWithPage(DBTransfer transfer)
        {
            object queryResult = null;
            if (transfer.IsPage)
            {
                if(!string.IsNullOrEmpty(transfer.SQL)&&string.IsNullOrEmpty(transfer.PageInfo.QueryName))
                {
                    transfer.PageInfo.QueryName = transfer.SQL;
                }
                queryResult = QueryPage(transfer);
                if(queryResult == null)
                {
                    //
                    if (string.IsNullOrEmpty(transfer.SQL))
                    {
                        //没有自定义查询
                        if (transfer.PageInfo.PageNum < 1)
                        {
                            transfer.SQL = queryPage.GetPageSql(transfer.DBCfg, transfer.PageInfo.QueryName, transfer.PageInfo.MinPage, transfer.PageInfo.MaxPage);
                        }
                        else
                        {
                            transfer.SQL = queryPage.GetPageSql(transfer.DBCfg, transfer.PageInfo.QueryName, transfer.PageInfo.PageNum);
                        }
                    }
                    var result=ServerSQL(transfer);
                    if(result.Error==ErrorCode.Sucess&&transfer.PageInfo.PageNum>0)
                    {
                        queryPage.Add(transfer.DBCfg, transfer.PageInfo.QueryName, transfer.PageInfo.PageNum, result.Result);
                    }
                    return result;
                }
                RequestResult qresult = new RequestResult();
                qresult.Result = queryResult;
                qresult.Error = ErrorCode.Sucess;
                return qresult;
            }
            else
            {
                return ServerSQL(transfer);
            }
        }

        private RequestResult KV(DBTransfer transfer)
        {
            RequestResult result =new RequestResult();
            result.Error = ErrorCode.Sucess;
            try
            {
                switch (transfer.SQL)
                {
                    case "Put":
                        bDBHelper.PutKVS(transfer.Paramter);
                        break;
                    case "Delete":
                        object[] keys = new object[transfer.Paramter.Count];
                        transfer.Paramter.Keys.CopyTo(keys, 0);
                        bDBHelper.DeleteList(keys);
                        break;
                    case "Get":
                        HashSet<object> set = new HashSet<object>(transfer.Paramter.Keys);
                        result.Result = bDBHelper.GetKVS(set);
                        break;
                    case "Clear":
                        bDBHelper.Clear();
                        break;
                }
            }
            catch(Exception ex)
            {
                result.Error = ErrorCode.Exception;
                result.ReslutMsg = ex.Message;
            }
            return result;
        }

        private RequestResult LocalSQL(DBTransfer transfer)
        {
            RequestResult result = new RequestResult();
            result.Error = ErrorCode.Sucess;
            DBAcessPool dBAcess = new DBAcessPool();
            dBAcess.DBName = transfer.DBCfg;
            if (transfer.IsQuery)
            {
                try
                {
                    DataSet ds = sqliteHelper.GetSelect(transfer.SQL);
                    sqliteHelper.Close();
                    if (ds.Tables.Count > 0)
                    {
                        DataTable dt = ds.Tables[0];
                        result.Result = dt;
                        if (transfer.IsModel && !string.IsNullOrEmpty(transfer.ModelCls))
                        {
                            object value = null;
                            if (!dicModels.TryGetValue(transfer.ModelCls, out value))
                            {

                                string path = null;
                                path = transfer.ModelDLL;
                                if (string.IsNullOrEmpty(path))
                                {
                                    path = transfer.ModelCls.Substring(0, transfer.ModelCls.LastIndexOf("."));
                                }
                                Assembly assembly = Assembly.LoadFrom(Path.Combine("Models", path, ".dll"));
                                value = assembly.CreateInstance(transfer.ModelCls, true);
                            }
                            //
                            try
                            {
                                result.Result = DataConvert<object>.ToList(dt);
                            }
                            catch (Exception ex)
                            {
                                result.Error = ErrorCode.Exception;
                                result.ReslutMsg = "转换Model错误，" + ex.Message;
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    result.Error = ErrorCode.Exception;
                    result.ReslutMsg = "查询错误，" + ex.Message;
                }
            }
            else
            {
                try
                {
                    int r = sqliteHelper.ExecuteUpdate(transfer.SQL, transfer.IsScala);
                    result.Result = r;
                    sqliteHelper.Close();
                }
                catch (Exception ex)
                {
                    result.Error = ErrorCode.Exception;
                    result.ReslutMsg = "执行错误，" + ex.Message;
                }

            }
            return result;
        }

        private RequestResult NoSQL(DBTransfer transfer)
        {
            RequestResult result = new RequestResult
            {
                Error = ErrorCode.Sucess
            };
            try
            {
                switch (transfer.SQL)
                {
                    case "Put":
                        Redis.Instance.Insert(transfer.Paramter);
                        break;
                    case "Delete":
                        {
                            object[] keys = new object[transfer.Paramter.Count];
                            transfer.Paramter.Keys.CopyTo(keys, 0);
                            Redis.Instance.Remove(keys);
                        }
                        break;
                    case "Get":
                        {
                            object[] keys = new object[transfer.Paramter.Count];
                            transfer.Paramter.Keys.CopyTo(keys, 0);
                            result.Result = Redis.Instance.GetKVS<object>(keys);
                        }
                        break;
                    case "Clear":
                        Redis.Instance.Clear();
                        break;
                }
            }
            catch(Exception ex)
            {
                result.Error = ErrorCode.Exception;
                result.ReslutMsg = ex.Message;
            }
            return result;
        }

        private RequestResult MemorySQL(DBTransfer transfer)
        {
            RequestResult result = new RequestResult();
            result.Error = ErrorCode.Sucess;
            DBAcessPool dBAcess = new DBAcessPool();
            dBAcess.DBName = transfer.DBCfg;
            if (transfer.IsQuery)
            {
                try
                {
                    DataSet ds = memHelper.GetSelect(transfer.SQL);

                    if (ds.Tables.Count > 0)
                    {
                        DataTable dt = ds.Tables[0];
                        result.Result = dt;
                        if (transfer.IsModel && !string.IsNullOrEmpty(transfer.ModelCls))
                        {
                            object value = null;
                            if (!dicModels.TryGetValue(transfer.ModelCls, out value))
                            {

                                string path = null;
                                path = transfer.ModelDLL;
                                if (string.IsNullOrEmpty(path))
                                {
                                    path = transfer.ModelCls.Substring(0, transfer.ModelCls.LastIndexOf("."));
                                }
                                Assembly assembly = Assembly.LoadFrom(Path.Combine("Models", path, ".dll"));
                                value = assembly.CreateInstance(transfer.ModelCls, true);
                            }
                            //
                            try
                            {
                                result.Result = DataConvert<object>.ToList(dt);
                            }
                            catch (Exception ex)
                            {
                                result.Error = ErrorCode.Exception;
                                result.ReslutMsg = "转换Model错误，" + ex.Message;
                            }
                        }

                    }

                }
                catch (Exception ex)
                {
                    result.Error = ErrorCode.Exception;
                    result.ReslutMsg = "查询错误，" + ex.Message;
                }
            }
            else
            {
                try
                {
                    int r = memHelper.ExecuteUpdate(transfer.SQL, transfer.IsScala);
                    result.Result = r;
                }
                catch (Exception ex)
                {
                    result.Error = ErrorCode.Exception;
                    result.ReslutMsg = "执行错误，" + ex.Message;
                }

            }
            return result;
        }

        private RequestResult MemoryNoSQL(DBTransfer transfer)
        {
            RequestResult result = new RequestResult();
            result.Error = ErrorCode.Sucess;
            try
            {
                switch (transfer.SQL)
                {
                    case "Put":
                        localCache.Insert(transfer.Paramter);
                        break;
                    case "Delete":
                        {
                            object[] keys = new object[transfer.Paramter.Count];
                            transfer.Paramter.Keys.CopyTo(keys, 0);
                            localCache.Remove(keys);
                        }
                        break;
                    case "Get":
                        {
                            object[] keys = new object[transfer.Paramter.Count];
                            transfer.Paramter.Keys.CopyTo(keys, 0);
                            result.Result = localCache.GetValues(keys);
                        }
                        break;
                    case "Clear":
                        localCache.Clear();
                        break;
                }
            }
            catch (Exception ex)
            {
                result.Error = ErrorCode.Exception;
                result.ReslutMsg = ex.Message;
            }
            return result;
        }


    }
}
