using BerkeleyDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BDB
{

    /// <summary>
    /// 基础应用
    /// </summary>
   public abstract  class BDBOpt
    {
        /// <summary>
        /// 数据库目录
        /// </summary>
        private string data_dir= "envBDBData";

        /// <summary>
        /// 数据库文件名
        /// </summary>
         protected string home= "envBDBHome";


        private string temp_dir = "BDBTemp";

        private string db_blobdir = "envBDBBlob";

        private string db_logdir = "envBDBLog";

        private string db_backupdir = "BDBBackUP";

        private string DBRootDir = "";

        private DatabaseEnvironment env = null;
        private Database db = null;
        Sequence seq;
        SequenceConfig seqConfig;
        const int EXIT_FAILURE = 1;
        const int EXIT_SUCCESS = 0;
        const string progName = "excs_env";
        private readonly int bufferSize=1024*1024;//1M
        DatabaseConfig databaseConfig = null;

        protected DBType DBType=DBType.BTree;

        protected string db_File = "dbd.db";

        private string dbFileEx = ".db";

       

        protected string dbName = "dbd";
        private uint mPort;

        public string EnvHome { get { return home; } set { home=value; } }

        /*
         * Set up environment.
         */
        public  int SetUpEnv(string home, string data_dir)
        {
            CheckDir();
            LogConfig logCfg = new LogConfig();
            logCfg.AutoRemove = true;
            logCfg.Dir =db_logdir;
            logCfg.NoSync = true;
            logCfg.RegionSize = 1024 * 1024 * 5;
            DatabaseEnvironmentConfig envConfig;
            /* Configure an environment. */
            envConfig = new DatabaseEnvironmentConfig();
            envConfig.MPoolSystemCfg = new MPoolConfig();
            envConfig.MPoolSystemCfg.CacheSize = new CacheInfo(
                0, 64 * 1024, 1);
            envConfig.Create = true;
            envConfig.DataDirs.Add(data_dir);
           
            envConfig.CreationDir = data_dir;
            envConfig.ErrorPrefix = progName;
            envConfig.UseLogging = true;
            envConfig.UseLocking = true;
            envConfig.UseMPool = true;
            envConfig.UseTxns = true;
            envConfig.AutoCommit = true;
            envConfig.BlobDir = db_blobdir;
            envConfig.TempDir = temp_dir;
            envConfig.UseMVCC = true;
            envConfig.LogSystemCfg = logCfg;
            
            /* Create and open the environment. */
            try
            {
                env = DatabaseEnvironment.Open(home, envConfig);
              

            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
                return EXIT_FAILURE;
            }

          
            return EXIT_SUCCESS;
        }


        private void CheckDir()
        {
           
            if (!Directory.Exists(home))
            {
                Directory.CreateDirectory(home);
            }
            if (!Directory.Exists(Path.Combine(home, db_logdir)))
            {
                Directory.CreateDirectory(Path.Combine(home, db_logdir));
            }//监测目录
            if (!Directory.Exists(Path.Combine(home, data_dir)))
            {
                Directory.CreateDirectory(Path.Combine(home, data_dir));
            }
            //
            if (!Directory.Exists(Path.Combine(home, temp_dir)))
            {
                Directory.CreateDirectory(Path.Combine(home, temp_dir));
            }
            //
            if (!Directory.Exists(Path.Combine(home, db_blobdir)))
            {
                Directory.CreateDirectory(Path.Combine(home, db_blobdir));
            }
        }


        /*
         * Tear down environment and remove its files.
         * Any log or database files and the environment 
         * directory are not removed.
         */
        public static int TearDownEnv(string home=null)
        {
            /* Remove environment regions. */
            if(home==null)
            {
                home = "envBDBHome";
            }
            try
            {
                DatabaseEnvironment.Remove(home);
                
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}: {1}\n{2}",
                    e.Source, e.Message, e.StackTrace);
                return EXIT_FAILURE;
            }

            return EXIT_SUCCESS;
        }

        /// <summary>
        /// 移除数据
        /// </summary>
        /// <param name="db"></param>
        public  void RemoveDB(string db)
        {
            if(string.IsNullOrWhiteSpace(db))
            {
                db= dbName; 
            }
            env.RemoveDB(db + dbFileEx,db,true);
           
        }

        /// <summary>
        /// 备份数据库数据
        /// </summary>
        /// <param name="dir"></param>
        public void SetBackUP(string dir)
        {
            if(string.IsNullOrWhiteSpace(dir))
            {
                dir = db_backupdir;
            }
            BackupOptions options = new BackupOptions();
            options.Creation = CreatePolicy.IF_NEEDED;
            options.Files = true;
            options.NoLogs = true;
            env.Backup(dir, options);
        }

       /// <summary>
       /// 增量备份
       /// </summary>
       /// <param name="dir"></param>
        public void SetincrementalBackUP(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = db_backupdir;
            }
            BackupOptions options = new BackupOptions();
            options.Creation = CreatePolicy.IF_NEEDED;
            options.Files = true;
            options.Update = true;
            env.Backup(dir, options);
        }

        public static void usage()
        {
            Console.WriteLine("Usage: excs_env [home] [data dir]");
        }

        /// <summary>
        /// 初始化
        /// </summary>
        protected bool Init()
        {
            //
            try
            {
                String pwd = Environment.CurrentDirectory;
                //pwd = Path.Combine(pwd, "..");
                // pwd = Path.Combine(pwd, "..");
                if (IntPtr.Size == 4)
                    pwd = Path.Combine(pwd, "Win32");
                else
                    pwd = Path.Combine(pwd, "x64");
                //#if DEBUG
                //                pwd = Path.Combine(pwd, "Debug");
                //#else
                //                pwd = Path.Combine(pwd, "Release");
                //#endif
                pwd += ";" + Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", pwd);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Unable to set the PATH environment variable.");
                Console.WriteLine(e.Message);
                return false;
            }
            //
          
            /* Set up environment. */
            if (SetUpEnv(home, data_dir) == EXIT_FAILURE)
            {
                Console.WriteLine("Fail to set up the environment.");
                return false;
            }
            Console.WriteLine("Set up the environment.");
            if(!Directory.Exists(db_blobdir))
            {
                Directory.CreateDirectory(db_blobdir);
            }
          
            /* Configure the database. */
            switch (DBType)
            {
                case DBType.BTree:
                case DBType.Sequence:
                    {
                        BTreeDatabaseConfig config = new BTreeDatabaseConfig();
                        config.Duplicates = DuplicatesPolicy.SORTED;
                        config.Creation = CreatePolicy.IF_NEEDED;
                        config.ExternalFileDir = db_blobdir;
                      
                        databaseConfig = config;
                    }
                    break;
                case DBType.Hash:
                    {
                        HashDatabaseConfig config = new HashDatabaseConfig();
                        config.Creation = CreatePolicy.IF_NEEDED;
                        config.Duplicates = DuplicatesPolicy.SORTED;
                        config.CacheSize = new CacheInfo(0, 64 * 1024, 1);
                        config.BlobDir = db_blobdir;
                        databaseConfig = config;
                    }
                    break;
                case DBType.Recno:
                    {
                        RecnoDatabaseConfig config = new RecnoDatabaseConfig();
                        config.Creation = CreatePolicy.IF_NEEDED;
                        config.Length = 1000;
                        databaseConfig = config;
                    }
                    break;
                case DBType.Queue:
                    {
                        QueueDatabaseConfig config = new QueueDatabaseConfig();
                        config.Creation = CreatePolicy.IF_NEEDED;
                        databaseConfig = config;
                    }
                    break;
                default:
                    databaseConfig = new BTreeDatabaseConfig();
                    break;
            }
          
            databaseConfig.ErrorPrefix = "excs_access";
            databaseConfig.PageSize = 8 * 1024;
            databaseConfig.Env = env;
           
           
            try
            {
                /* Create and open a new database in the file. */
                CreateDB(databaseConfig);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error opening {0}.", db_File);
                Console.WriteLine(e.Message);
                return false;
            }

        }

        private void CreateDB(DatabaseConfig config)
        {
            
            string dbFile = Path.Combine(home,  dbName+dbFileEx);
            db_File = dbFile = dbName + dbFileEx;
            dbFile = Path.Combine(Environment.CurrentDirectory, db_File);
            switch (DBType)
            {
                case DBType.BTree:
                case DBType.Sequence:
                    {

                        BTreeDatabaseConfig dbcfg = config as BTreeDatabaseConfig;
                       
                        if(DBType==DBType.Sequence)
                        {
                            dbcfg.Duplicates = DuplicatesPolicy.NONE;
                        }
                        db = BTreeDatabase.Open(db_File,dbName, dbcfg);
                        if (dbcfg.Duplicates!=DuplicatesPolicy.SORTED)
                        {
                            /* Configure and initialize sequence. */
                            seqConfig = new SequenceConfig();
                            seqConfig.BackingDatabase = db;
                            seqConfig.Creation = CreatePolicy.IF_NEEDED;
                            seqConfig.Increment = true;
                            seqConfig.InitialValue = Int64.MaxValue;
                            seqConfig.key = new DatabaseEntry();
                            seqConfig.SetRange(Int64.MinValue, Int64.MaxValue);
                            seqConfig.Wrap = true;
                            DbtFromString(seqConfig.key, "excs_sequence");
                            seq = new Sequence(seqConfig);
                        }
                    }
                    break;
                case DBType.Hash:
                    db = HashDatabase.Open(dbFile, config as HashDatabaseConfig);
                    break;
                case DBType.Recno:
                    db = RecnoDatabase.Open(dbFile, config as RecnoDatabaseConfig);
                    break;
                case DBType.Queue:
                    db = QueueDatabase.Open(dbFile, config as QueueDatabaseConfig);
                    break;
                default:
                    db = BTreeDatabase.Open(dbFile, config as BTreeDatabaseConfig);
                    break;

            }
        }
        #region Utilities

        public static void DbtFromString(DatabaseEntry dbt, string s)
        {
            dbt.Data = System.Text.Encoding.ASCII.GetBytes(s);
        }

        static void dbtFromString(DatabaseEntry dbt, string s)
        {
            dbt.Data = System.Text.Encoding.ASCII.GetBytes(s);
        }

        public static string strFromDBT(DatabaseEntry dbt)
        {

            System.Text.ASCIIEncoding decode =
                new ASCIIEncoding();
            return decode.GetString(dbt.Data);
        }

        public static string reverse(string s)
        {
            StringBuilder tmp = new StringBuilder(s.Length);
            for (int i = s.Length - 1; i >= 0; i--)
                tmp.Append(s[i]);
            return tmp.ToString();
        }

        #endregion Utilities


        #region

        /// <summary>
        /// 直接存储数据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="txn"></param>
        public void Put(byte[] key,byte[] value, Transaction txn=null)
        {
            DatabaseEntry k = new DatabaseEntry(key);
            DatabaseEntry v = new DatabaseEntry(value);

            db.Put(k, v, txn);
            
        }

        /// <summary>
        /// 没有存在key才存储
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="txn"></param>
        public void PutNoOverwrite(byte[] key, byte[] value, Transaction txn = null)
        {
            DatabaseEntry k = new DatabaseEntry(key);
            DatabaseEntry v = new DatabaseEntry(value);

            db.PutNoOverwrite(k, v, txn);
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="info"></param>
        /// <param name="txn"></param>
        /// <returns></returns>
        public byte[] Get(byte[] key,LockingInfo info=null, Transaction txn = null)
        {
            DatabaseEntry k = new DatabaseEntry(key);
             KeyValuePair<DatabaseEntry,DatabaseEntry> pair= db.Get(k,txn,info);
            byte[] tmp = new byte[pair.Value.Data.Length];
            Array.Copy(pair.Value.Data, tmp, tmp.Length);
            pair.Key.Dispose();
            pair.Value.Dispose();
            return tmp; 
        }

       /// <summary>
       /// 获取多个值
       /// </summary>
       /// <param name="keys"></param>
       /// <param name="info"></param>
       /// <param name="txn"></param>
       /// <returns></returns>
        public Dictionary<byte[],byte[]> Get(List<byte[]> keys, LockingInfo info = null, Transaction txn = null)
        {
            Dictionary<byte[], byte[]> result = new Dictionary<byte[], byte[]>();
            foreach (byte[] key in keys)
            {
                DatabaseEntry k = new DatabaseEntry(key);
                KeyValuePair<DatabaseEntry, DatabaseEntry> pair = db.GetBoth(k, null, txn, info);
                byte[] tmp = new byte[pair.Value.Data.Length];
                Array.Copy(pair.Value.Data, tmp, tmp.Length);
                result[key] = tmp;
                pair.Key.Dispose();
                pair.Value.Dispose();
            }
            return result;
        }

        //public Dictionary<byte[], byte[]> GetMult(List<byte[]> keys, LockingInfo info = null, Transaction txn = null)
        //{
        //    Dictionary<byte[], byte[]> result = new Dictionary<byte[], byte[]>();
        //    // List<KeyValuePair<DatabaseEntry, DatabaseEntry>> lst = new List<KeyValuePair<DatabaseEntry, DatabaseEntry>>();
        //    List<DatabaseEntry> lst = new List<DatabaseEntry>();
        //    foreach (byte[] key in keys)
        //    {
        //        DatabaseEntry k = new DatabaseEntry(key);

        //        lst.Add(k);
        //    }
        //    MultipleDatabaseEntry multiple = new MultipleDatabaseEntry(lst, false);
        //    KeyValuePair<DatabaseEntry,DatabaseEntry> kv= db.Get(multiple);
           
        //    return result;
        //}

        /// <summary>
        ///取出多个值
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="info"></param>
        /// <param name="txn"></param>
        /// <returns></returns>
        public Dictionary<byte[], byte[]> GetMultiple(List<byte[]> keys, LockingInfo info = null, Transaction txn = null)
        {
            Dictionary<byte[], byte[]> result = new Dictionary<byte[], byte[]>();
            foreach (byte[] key in keys)
            {
                DatabaseEntry k = new DatabaseEntry(key);
                KeyValuePair<DatabaseEntry, MultipleDatabaseEntry> pair = db.GetMultiple(k,bufferSize, txn, info);
                result[key] = pair.Value.Data;
            }
            return result;
        }

        public void Delete(byte[]key, Transaction txn=null)
        {
            DatabaseEntry k = new DatabaseEntry(key);
            db.Delete(k, txn);
        }

        public void Delete(List<byte[]> keys)
        {
            List<DatabaseEntry> lst = new List<DatabaseEntry>(keys.Count);
            foreach(byte[] k in keys )
            {
                DatabaseEntry entry = new DatabaseEntry(k);
                lst.Add(entry);
            }
            MultipleDatabaseEntry mk = new MultipleDatabaseEntry(lst,false);
            db.Delete(mk);
          
        }

        /// <summary>
        /// 清除所有数据
        /// </summary>
        public void Clear()
        {
            db.Truncate();
        }
        /// <summary>
        /// 关闭数据库
        /// </summary>
        public void Close()
        {
            db.Close();
            db.Dispose();
        }
        
        /// <summary>
        /// 返回游标
        /// </summary>
        /// <param name="config"></param>
        /// <param name="txn"></param>
        /// <returns></returns>
        public Cursor OpenCursor(CursorConfig config=null, Transaction txn = null)
        {
            if(config==null)
            {
                config = new CursorConfig();
            }
           return db.Cursor(config, txn);
        }
       
        public Transaction GetTransaction()
        {
            Transaction transaction = env.BeginTransaction();
            return transaction;
        }
        
        public void Commit(Transaction txn)
        {
            txn.Commit();
           
        }
        
        public Dictionary<byte[],byte[]> GetALL()
        {
            Dictionary<byte[], byte[]> result = new Dictionary<byte[], byte[]>();
            Cursor cursor=  db.Cursor();
            while(cursor.MoveNext())
            {
                DatabaseEntry key = cursor.Current.Key;
                DatabaseEntry value = cursor.Current.Value;
                byte[] k = new byte[key.Data.Length];
                byte[] v = new byte[value.Data.Length];
                Array.Copy(key.Data, k, k.Length);
                Array.Copy(value.Data, v, v.Length);
                result[k] = v;

            }
            cursor.Close();
            cursor.Dispose();
            return result;
        }
        /// <summary>
        /// 获取序列
        /// 数据库类型必须是BTree
        /// </summary>
        /// <returns></returns>
        public long GetSequence()
        {
            if (null != seq)
            {
                return seq.Get(2);
            }
            else
            {
                throw new Exception("序列数据库没有创建");
            }
        }

        public void PutRecio(byte[] data, Transaction txn = null)
        {
            RecnoDatabase recno = db as RecnoDatabase;
            if (null != recno)
            {
                DatabaseEntry v = new DatabaseEntry(data);
                recno.Append(v, txn);
                
            }
        }
        public void PutQueue(byte[]data,Transaction txn=null)
        {
            QueueDatabase queue = db as QueueDatabase;
            if(null!=queue)
            {
                DatabaseEntry v = new DatabaseEntry(data);
                queue.Append(v, txn);
            }
        }
        public byte[] TakeQueue()
        {

            byte[] data = null;
            Cursor cursour=  db.Cursor();
           if(cursour.MoveFirst())
            {
               
                data = new byte[cursour.Current.Value.Data.Length];
                Array.Copy(cursour.Current.Value.Data, data, data.Length);
                cursour.Delete();
            }
            cursour.Close();
            cursour.Dispose();
            return data;
        }


        #region 只是演示复制，在环境初始化就必须完成，需要重构，这里不提供
        private void stuffHappened(NotificationEvent eventCode, byte[] info)
        {
            switch (eventCode)
            {
                case NotificationEvent.REP_AUTOTAKEOVER:
                    Console.WriteLine("Event: REP_AUTOTAKEOVER");
                    break;
                case NotificationEvent.REP_AUTOTAKEOVER_FAILED:
                    Console.WriteLine("Event: REP_AUTOTAKEOVER_FAILED");
                    break;
                case NotificationEvent.REP_CLIENT:
                    Console.WriteLine("Event: CLIENT");
                    break;
                case NotificationEvent.REP_CONNECT_BROKEN:
                    Console.WriteLine("Event: REP_CONNECT_BROKEN");
                    break;
                case NotificationEvent.REP_CONNECT_ESTD:
                    Console.WriteLine("Event: REP_CONNECT_ESTD");
                    break;
                case NotificationEvent.REP_CONNECT_TRY_FAILED:
                    Console.WriteLine("Event: REP_CONNECT_TRY_FAILED");
                    break;
                case NotificationEvent.REP_MASTER:
                    Console.WriteLine("Event: MASTER");
                    break;
                case NotificationEvent.REP_NEWMASTER:
                  //  electionDone = true;
                    Console.WriteLine("Event: NEWMASTER");
                    break;
                case NotificationEvent.REP_LOCAL_SITE_REMOVED:
                    Console.WriteLine("Event: REP_LOCAL_SITE_REMOVED");
                    break;
                case NotificationEvent.REP_SITE_ADDED:
                    Console.WriteLine("Event: REP_SITE_ADDED");
                    break;
                case NotificationEvent.REP_SITE_REMOVED:
                    Console.WriteLine("Event: REP_SITE_REMOVED");
                    break;
                case NotificationEvent.REP_STARTUPDONE:
                   // startUpDone++;
                    Console.WriteLine("Event: REP_STARTUPDONE");
                    break;
                case NotificationEvent.REP_PERM_FAILED:
                    Console.WriteLine("Event: Insufficient Acks.");
                    break;
                default:
                    Console.WriteLine("Event: {0}", eventCode);
                    break;
            }
        }
        private void StartRe(DatabaseEnvironmentConfig cfg)
        {
            cfg.EventNotify = new EventNotifyDelegate(stuffHappened);

            cfg.RepSystemCfg = new ReplicationConfig();
            cfg.RepSystemCfg.RepmgrSitesConfig.Add(new DbSiteConfig());
            cfg.RepSystemCfg.RepmgrSitesConfig[0].Host = "::1";
            cfg.RepSystemCfg.RepmgrSitesConfig[0].Port = mPort;
            cfg.RepSystemCfg.RepmgrSitesConfig[0].LocalSite = true;
            cfg.RepSystemCfg.RepmgrSitesConfig[0].GroupCreator = true;
            cfg.RepSystemCfg.Priority = 100;
            env.DeadlockResolution = DeadlockPolicy.DEFAULT;
            env.RepMgrStartMaster(2);
        }
        private void ReApplication(string home,DatabaseEnvironmentConfig cfg,uint port,string db,DatabaseConfig dbcfg)
        {
            cfg.RepSystemCfg.RepmgrSitesConfig[0].Port = port;
            cfg.RepSystemCfg.RepmgrSitesConfig[0].GroupCreator = false;
            cfg.RepSystemCfg.Priority = 10;
            cfg.RepSystemCfg.RepmgrSitesConfig.Add(new DbSiteConfig());
            cfg.RepSystemCfg.RepmgrSitesConfig[1].Host = "::1";
            cfg.RepSystemCfg.RepmgrSitesConfig[1].Port = mPort;
            cfg.RepSystemCfg.RepmgrSitesConfig[1].Helper = true;
           // cfg.RepSystemCfg.ReplicationView = 0;
            DatabaseEnvironment mEnv = DatabaseEnvironment.Open(home, cfg);
            dbcfg.Env = mEnv;
            //dbcfg.Creation = CreatePolicy.ALWAYS;
            dbcfg.AutoCommit = true;
            Database database = Database.Open(db + dbFileEx, dbcfg);
            mEnv.RepMgrStartClient(2, false);


        }
        #endregion 
        #endregion
    }
}
