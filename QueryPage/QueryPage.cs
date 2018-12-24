#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：QueryPage
* 项目描述 ：
* 类 名 称 ：QueryPage
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Data;
using Serializer;
using System.Threading.Tasks;
using System.Threading;

namespace QueryPage
{
    /* ============================================================================== 
    * 功能描述：QueryPage  查询初始化,对外处理类
    * 创 建 者：jinyu 
    * 修 改 者：jinyu 
    * 创建日期：2018 
    * 修改日期：2018 
    * ==============================================================================*/

    /// <summary>
    /// 对外提供缓存
    /// </summary>
   public class QueryPage
    {
        public QueryPageConfig PageConfig { get; set; }
        private QueryCache queryCache = null;
        private ConcurrentDictionary<string, Dictionary<string, PageSQL>> dicPageQuery = null;
        private ConcurrentDictionary<string, Dictionary<string, DBQueryCache>> dicSQL = null;
        private static long QueryID = 0;//ID
        private ConcurrentQueue<CacheFile> queue = null;//缓存文件
        public const string CacheFileExtension = ".dat";
        private bool isFileRun = false;//正在处理文件
        private volatile bool isValidateRun = false;//正在处理有效性

        /// <summary>
        /// 单例属性
        /// </summary>
        public static QueryPage Instance { get { return instance; } }

        private static readonly QueryPage instance = new QueryPage();

        public QueryPage()
        {
            dicPageQuery = new ConcurrentDictionary<string, Dictionary<string, PageSQL>>();
            dicSQL = new ConcurrentDictionary<string, Dictionary<string, DBQueryCache>>();
            queryCache = new QueryCache();
            PageConfig = new QueryPageConfig();
            queue = new ConcurrentQueue<CacheFile>();
            PageConfig.LoadConfig();//加载一次默认
            if (!Directory.Exists(PageConfig.FileCacheDir))
            {
                Directory.CreateDirectory(PageConfig.FileCacheDir);

            }
            queryCache.PageCacheNum = PageConfig.PageCacheNum;
        }

        private static T DESerializer<T>(string strXML) where T : class
        {
            try
            {
                using (StringReader sr = new StringReader(strXML))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    return serializer.Deserialize(sr) as T;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 初始化配置
        /// </summary>
        public void Init()
        {
            //读取配置SQL
            if(PageConfig==null)
            {
                return; 
            }
            //
            if (!Directory.Exists(PageConfig.PageTemplate))
            {
                return;

            }
            if(PageConfig.IsCacheFile)
            {
                if (!Directory.Exists(PageConfig.FileCacheDir))
                {
                    Directory.CreateDirectory(PageConfig.FileCacheDir);

                }
            }
            string[] files = Directory.GetFileSystemEntries(PageConfig.PageTemplate, "*.xml|*.XML");
            if(files.Length>0)
            {
                foreach(string file in files)
                {
                    FileStream fs = new FileStream(file, FileMode.Open);
                    StreamReader sr = new StreamReader(fs);
                    string xml = sr.ReadToEnd();
                    string fileName = fs.Name;
                    sr.Close();
                    fs.Close();
                    QueryPageSQLXML data = DESerializer<QueryPageSQLXML>(xml);
                    if(data!=null&&data.DBSQLs!=null)
                    {
                        foreach(DBSQL sql in data.DBSQLs)
                        {
                            string db = sql.DBName;
                            if(string.IsNullOrEmpty(db))
                            {
                                db = fileName.Substring(0, fileName.Length - 4);
                            }
                            //
                            if (sql.PageSQLs == null||sql.PageSQLs.Count==0)
                            {
                                continue;
                            }
                            dicPageQuery[db] = new Dictionary<string, PageSQL>();
                            var cur = dicPageQuery[db];
                            foreach (PageSQL pagesql in sql.PageSQLs)
                            {
                                pagesql.ID = QueryID++;
                                cur[pagesql.QueryName] = pagesql;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 整理保持数据
        /// </summary>
        /// <param name="db"></param>
        /// <param name="queryID"></param>
        /// <param name="pageNum"></param>
        /// <param name="data"></param>
        /// <param name="isModel"></param>
        private void PutFile(string db,long queryID,int pageNum,object data,  bool isModel=false)
        {
            string flage = "T";
            if (isModel)
            {
                flage = "N";
            }
            CacheFile cacheFile = new CacheFile() { DB = db, Data = data, isModel = isModel };
            cacheFile.Name = string.Format("{0}_{1}_{2}", queryID, pageNum,flage);
            queue.Enqueue(cacheFile);
            if(!RemoveFile.Instance.IsRun)
            {
                RemoveFile.Instance.CacheFileDir = PageConfig.FileCacheDir;
                RemoveFile.Instance.Start();
            }
        }


        /// <summary>
        /// 从缓存文件中获取数据
        /// </summary>
        /// <param name="db"></param>
        /// <param name="queryID"></param>
        /// <param name="pageNum"></param>
        /// <param name="isModel"></param>
        /// <returns></returns>
        private object GetFile(string db,long queryID,int pageNum,bool isModel)
        {
            string flage = "T";
            if(isModel)
            {
                flage = "N";
            }
            string dir = Path.Combine(PageConfig.FileCacheDir, db);
            string[] files= Directory.GetFileSystemEntries(dir, queryID+"_"+pageNum+"_"+flage + CacheFileExtension);
            if(files.Length>0)
            {
                foreach (string file in files)
                {
                    using (FileStream fs = new FileStream(file, FileMode.Open))
                    {
                        byte[] bytes = new byte[fs.Length];
                        fs.Read(bytes, 0, bytes.Length);
                        if (!isModel)
                        {
                            return SerializerFactory<CommonSerializer>.Deserialize<DataTable>(bytes);
                        }
                        else
                        {
                            return SerializerFactory<CommonSerializer>.Deserialize<List<object>>(bytes);
                        }
                    }
                }
              
            }
            return null;
        }


        /// <summary>
        /// 保持文件
        /// </summary>
        private void SaveFile()
        {

            if(isFileRun)
            {
                return;
            }
            isFileRun = true;
            Task.Factory.StartNew(() =>
            {
                isFileRun = true;
                if (!queue.IsEmpty)
                {
                    CacheFile cacheFile = null;
                    do
                    {
                        if (queue.TryDequeue(out cacheFile))
                        {
                            byte[] bytes = null;
                            if (cacheFile.isModel)
                            {
                                bytes = SerializerFactory<CommonSerializer>.Serializer(cacheFile.Data);
                            }
                            else
                            {
                                bytes = SerializerFactory<CommonSerializer>.Serializer<DataTable>((DataTable)cacheFile.Data);
                            }
                            string dir = Path.Combine(PageConfig.FileCacheDir, cacheFile.DB);
                            string file = Path.Combine(dir, cacheFile.Name, CacheFileExtension);
                            using (FileStream fs = new FileStream(file, FileMode.Create))
                            {
                                try
                                {
                                    fs.BeginWrite(bytes, 0, bytes.Length, null, null);
                                }
                                catch(IOException ex)
                                {
                                    if(!Directory.Exists(dir))
                                    {
                                        Directory.CreateDirectory(dir);
                                        queue.Enqueue(cacheFile);
                                    }
                                }
                               catch(Exception ex)
                                {

                                }
                            }
                           
                        }
                    } while (!queue.IsEmpty);
                }
                isFileRun = false;
                SaveFile();//
            });
        }


        /// <summary>
        /// 触发验证
        /// </summary>
        private void Validate()
        {
            if (isValidateRun)
            {
                return;
            }
            isValidateRun = true;
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(PageConfig.CacheTime/2 * 60 * 1000);
                queryCache.Validate(PageConfig.CacheTime);
                isValidateRun = false;
                Validate();//自旋
            });
        }

        /// <summary>
        /// 获取SQL语句
        /// </summary>
        /// <param name="db"></param>
        /// <param name="queryName"></param>
        /// <param name="pageNum"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public string GetPageSql(string db,string queryName,int pageNum, int min=-1,int max=-1)
        {
            Dictionary<string, PageSQL> pageSQL = null;
            PageSQL page = null;
            if(dicPageQuery.TryGetValue(db,out pageSQL))
            {
                if(pageSQL.TryGetValue(queryName,out page))
                {
                    string sql = page.SQL;
                    if(pageNum>1)
                    {
                        sql= sql.Replace("@Min", ((pageNum - 1) * PageConfig.PageSize).ToString());
                        sql = sql.Replace("@Max", (pageNum* PageConfig.PageSize).ToString());
                        return sql;
                    }
                    else
                    {
                        sql = sql.Replace("@Min", min.ToString());
                        sql = sql.Replace("@Max", max.ToString());
                        return sql;
                    }
                }
            }
            return null;
        }

    /// <summary>
    /// 缓存数据
    /// </summary>
    /// <param name="db">数据库名称（配置名称）</param>
    /// <param name="queryName">业务查询名称</param>
    /// <param name="pageNum">页号</param>
    /// <param name="data">数据</param>
        public  void Add(string db,string queryName, int pageNum, object data)
        {
            if(!PageConfig.isMemory)
            {
                return;
            }
            Dictionary<string, PageSQL> pageSQL = null;
            PageSQL page = null;
            long id = -1;
            if (dicPageQuery.TryGetValue(db,out pageSQL))
            {
                if(pageSQL.TryGetValue(queryName,out page))
                {
                    id = page.ID;
                }
            }
            if (id == -1)
            {
                //说明没有配置，则是SQL
                Dictionary<string, DBQueryCache> cur = null;
                DBQueryCache cache = null;
                if (dicSQL.TryGetValue(db, out cur))
                {
                    if (cur.TryGetValue(queryName, out cache))
                    {

                        if (data is DataTable)
                        {
                            cache.Add(pageNum, (DataTable)data);

                        }
                        else
                        {
                            cache.Add(pageNum, (List<object>)data);

                        }

                    }
                    else
                    {
                        cache = new DBQueryCache(PageConfig.PageCacheNum);
                        cur[queryName] = cache;
                        if (data is DataTable)
                        {
                            cache.Add(pageNum, (DataTable)data);
                        }
                        else
                        {
                            cache.Add(pageNum, (List<object>)data);
                        }
                    }

                }
                else
                {
                    cache = new DBQueryCache(PageConfig.PageCacheNum);
                    if (data is DataTable)
                    {
                        cache.Add(pageNum, (DataTable)data);
                    }
                    else
                    {
                        cache.Add(pageNum, (List<object>)data);
                    }
                    cur = new Dictionary<string, DBQueryCache>();
                    cur[queryName] = cache;
                    dicSQL[db] = cur;
                }
            }
            else
            {
                object oldData = null;
                bool isModel = false;
                if (data is DataTable)
                {
                    oldData= queryCache.Add(db, id, pageNum, (DataTable)data);
                }
                else
                {
                    oldData=queryCache.Add(db, id, pageNum, (List<object>)data);
                    isModel = true;
                }
                if (PageConfig.IsCacheFile && oldData != null)
                {
                    PutFile(db, id, pageNum, oldData, isModel);
                    SaveFile();
                }
                Validate();
            }
        }

        /// <summary>
        /// 获取model
        /// </summary>
        /// <param name="db"></param>
        /// <param name="query"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public List<object> GetListModel(string db,string query,int pageNum)
        {
            if (!PageConfig.isMemory)
            {
                return null;
            }
            Dictionary<string, PageSQL> pageSQL = null;
            PageSQL page = null;
            if(dicPageQuery.TryGetValue(db,out pageSQL))
            {
                if(pageSQL.TryGetValue(query,out page))
                {
                    var lst=  queryCache.GetListModel(db, page.ID, pageNum);
                    if(lst==null&&PageConfig.IsCacheFile)
                    {
                       return (List<object>)GetFile(db, page.ID, pageNum, true);
                    }
                    return lst;
                }
            }
            else
            {
                Dictionary<string, DBQueryCache> cur = null;
                DBQueryCache cache = null;
                if(dicSQL.TryGetValue(db,out cur))
                {
                    if(cur.TryGetValue(query,out cache))
                    {
                       return  cache.GetListModel(pageNum);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="db"></param>
        /// <param name="query"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public DataTable GetDataTable(string db, string query, int pageNum)
        {
            if (!PageConfig.isMemory)
            {
                return null;
            }
            Dictionary<string, PageSQL> pageSQL = null;
            PageSQL page = null;
            if (dicPageQuery.TryGetValue(db, out pageSQL))
            {
                if (pageSQL.TryGetValue(query, out page))
                {
                    var dt= queryCache.GetDataTable(db, page.ID, pageNum);
                    if (dt == null && PageConfig.IsCacheFile)
                    {
                        return (DataTable)GetFile(db, page.ID, pageNum, true);
                    }
                    return dt;
                }
            }
            else
            {
                Dictionary<string, DBQueryCache> cur = null;
                DBQueryCache cache = null;
                if (dicSQL.TryGetValue(db, out cur))
                {
                    if (cur.TryGetValue(query, out cache))
                    {
                        return cache.GetDataTable(pageNum);
                    }
                }
            }
            return null;
        }

    }
}
