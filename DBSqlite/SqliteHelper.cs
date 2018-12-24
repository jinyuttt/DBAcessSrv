using ISQLDB;
using System.Data.SQLite;
using System.Data;
using System.IO;
using System;

namespace DBSqlite
{
    /// <summary>
    /// Sqlite操作，文件格式
    /// 优化插入步骤：1.采用事物批量操作；2，关闭同步；3，采用WAL模式（另外设置cacheszie）
    /// </summary>
    public class SqliteHelper: SQLAcess
    {
        private int maxSize=10;
        private   string offSynchronous = "PRAGMA  synchronous=0";
        private string normalSynchronous = "PRAGMA  synchronous=1";
        private  string modelDelete = "PRAGMA journal_mode=DELETE";
        private  string modelWal = "PRAGMA journal_mode=WAL";
        private  string checkPoint = "PRAGMA wal_autocheckpoint=";//1000
        // private  string threadModel = "0x00008000";
        private string autoVacuum = "PRAGMA auto_vacuum =";
        private string  cacheSize="PRAGMA cache_size =";
        private string defaultSzie = "PRAGMA  default_cache_size=";
        private string pageSize = "PRAGMA page_size=";
        private string mmapSize = "PRAGMA mmap_size=";

        /// <summary>
        /// 单例模式
        /// 内存数据库时可以直接使用
        /// </summary>
        public readonly static SqliteHelper instance = new SqliteHelper();
        private bool isMemory = false;
        private SQLiteConnection memconnection = null;//内存数据库单例使用
        private IDbCommand dbCommand = null;

        /// <summary>
        /// 当前连接
        /// </summary>
        private SQLiteConnection Current { get; set; }


        /// <summary>
        /// 默认内存
        /// </summary>
        public SqliteHelper()
        {
            ConnectString = ":memory";
            isMemory = true;
            memconnection = new SQLiteConnection("Data Source=:memory:");
            Current = memconnection;
        }


        /// <summary>
        /// 创建空数据库
        /// </summary>
        /// <param name="db"></param>
        public void CreateEmptyDB(string db)
        {
            SQLiteConnection.CreateFile(db);
            ConnectString = db;
        }
        #region Sqlite设置优化

        /// <summary>
        /// 关闭同步，提高插入性能
        /// 在系统崩溃才有影响，程序崩溃不会
        /// </summary>
        public void SetOffSynchronous()
        {
            this.ExecuteUpdate(offSynchronous);
        }

        /// <summary>
        /// 采用WAL模式，在读写都需要的情况下提高性能
        /// 锁定会阻塞写但是不阻塞读
        /// </summary>
        /// <param name="checkPoint"></param>
        public void SetModelWal(int checkPoint=10000)
        {
            this.ExecuteUpdate(modelWal);
            this.ExecuteUpdate(checkPoint+checkPoint.ToString());
        }

       /// <summary>
       /// WAL模式下设置checkpoint，越大性能越高
       /// 但是同步低
       /// </summary>
       /// <param name="size"></param>
        public void SetCheckWalPoint(int size=1000)
        {
            this.ExecuteUpdate(checkPoint + size.ToString());
        }

        /// <summary>
        /// 设置Delete模式
        /// 该模式在更新频繁下高，默认模式
        /// </summary>
        public void SetModelDelete()
        {
            this.ExecuteUpdate(modelWal);
        }

        /// <summary>
        /// 设置自动清理，开启会降低性能
        /// </summary>
        /// <param name="isAuto"></param>
        public void SetAutoVACUUM(bool isAuto)
        {
            string cur = "";
            if(isAuto)
            {
                cur = autoVacuum + " 1";
            }
            else
            {
                cur = autoVacuum + " 0";
            }
            this.ExecuteUpdate(cur);
        }

        /// <summary>
        /// 设置CacheSize
        /// 内存允许越大越好
        /// 只针对当前连接
        /// </summary>
        /// <param name="size"></param>
        public void SetCacheSize(int size=256*1024)
        {
            string cur = "";
            cur = cacheSize + size;
            this.ExecuteUpdate(cur);
        }

        /// <summary>
        ///设置CacheSize
        /// 内存允许越大越好
        /// 针对整个数据库
        /// </summary>
        /// <param name="size"></param>
        public void SetDefaultCacheSize(int size)
        {
            string cur = "";
            cur = defaultSzie + size;
            this.ExecuteUpdate(cur);
        }

        /// <summary>
        /// 执行Vacuum命令，清理文件内容
        ///非常非常影响性能
        ///最好是没有任何操作时执行
        ///比如程序启动，关闭或者业务判断很久不会有操作
        /// </summary>
        public void Vacuum()
        {
            this.ExecuteUpdate("PRAGMA Vacuum");
        }

        /// <summary>
        /// 设置内存映射大小
        /// 数据库默认22
        /// 不熟悉不要设置，反复测试
        /// </summary>
        /// <param name="size"></param>
        public void SetMapSize(int size=44)
        {
            this.ExecuteUpdate(mmapSize+size.ToString());
        }
        #endregion

        /// <summary>
        /// 数据库拷贝
        /// </summary>
        /// <param name="db">当前目的数据库</param>
        /// <param name="copy">需要拷贝的数据库</param>
        public void BackupS(IDbConnection db,IDbConnection copy)
        {
            if (ConnectionState.Open!=db.State)
            {
                db.Open();
            }
           if(ConnectionState.Open!=copy.State)
            {
                copy.Open();
            }
            SQLiteConnection sQLite = db as SQLiteConnection;
            SQLiteConnection sQLiteConnection = copy as SQLiteConnection;
            sQLite.BackupDatabase(sQLiteConnection, "main", "main", -1, null, -1);
        }

        /// <summary>
        /// 数据库拷贝
        /// 将外部的数据库拷贝本地
        /// </summary>
        /// <param name="db">将要拷贝的数据库</param>
        public void BackupS(IDbConnection db)
        {
            if (ConnectionState.Open != db.State)
            {
                db.Open();
            }
           
            SQLiteConnection sQLite = db as SQLiteConnection;
            SQLiteConnection sQLiteConnection = Current as SQLiteConnection;
            sQLiteConnection.BackupDatabase(sQLite, "main", "main", -1, null, -1);
        }


        public override IDbConnection NewConnect()
        {
            if (!isMemory)
            {
                SQLiteConnection cn = (SQLiteConnection)SQLiteFactory.Instance.CreateConnection();
                cn.ConnectionString = string.Format("Data Source={0};Version=3;Pooling=true;FailIfMissing=false;", ConnectString);
                // SQLiteConnection cn = new SQLiteConnection(string.Format("Data Source={0};Version=3;Pooling=true;FailIfMissing=false;", ConnectString));
                cn.Open();
                Current = cn;
                return cn;
            }
            else
            {
                return memconnection;
            }
        }
        public override IDbCommand NewCommand(IDbConnection connection)
        {
          return   connection.CreateCommand();
        }
        public override IDbCommand CreateCommand()
        {
            return NewCommand(NewConnect());
        }

        public override IDbCommand NewCommand(IDbConnection connection, string sql)
        {
            IDbCommand command = NewCommand(connection);
            command.CommandText = sql;
            return command;
        }

        public override IDbCommand NewCommand(string sql)
        {
            IDbCommand command = CreateCommand();
            command.CommandText = sql;
            return command;
        }

        protected override IDbConnection GetConnectionPool()
        {
          
            return NewConnect();
        }

        public override IDataReader GetDataReader(IDbConnection connection, string sql)
        {

            IDbCommand command = NewCommand(connection);
            command.CommandText = sql;
            IDataReader reader= command.ExecuteReader();
            dbCommand = command;
            return reader;
        }

        public override IDataReader GetDataReader(string sql)
        {
            return GetDataReader(GetConnectionPool(), sql);
        }

        public override DataSet GetSelect(string sql)
        {
            DataSet ds = new DataSet();
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(sql,(SQLiteConnection) GetConnectionPool());
            adapter.Fill(ds);
            adapter.Dispose();
            return ds;
        }

        public override void InitPool()
        {
           
        }

        public override int ExecuteUpdate(string sql,bool scalar=false)
        {
            IDbCommand command = NewCommand(sql);
            int r = 0;
            if (scalar)
            {
                 r = (int)command.ExecuteScalar();
               
            }
            else
            {
                r= command.ExecuteNonQuery();
            }
            command.Dispose();
            return r;

            
        }

        public override IDbTransaction GetTransaction()
        {
            return NewConnect().BeginTransaction();

        }
        public override IDbDataParameter GetDataParameter()
        {
            return new SQLiteParameter();
        }

        public override void RemoveDB()
        {
            try
            {
                File.Delete(ConnectString);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// 关闭当前使用的连接
        /// </summary>
        public override void Close()
        {
           if(Current!=null)
            {
                Current.Close();
                Current = null;
            }
           if(dbCommand!=null)
            {
                dbCommand.Dispose();
                dbCommand = null;
            }
        }

        public override void ClosePool()
        {
         
        }

        
    }
}
