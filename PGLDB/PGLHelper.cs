using ISQLDB;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using DBModel;

namespace PGLDB
{
   public class PGLHelper: SQLAcess
    {
        //private string conString = "Host=localhost;Port=5432;Username=postgres;Password=123;Database=postgres;Pooling=true;Maximum Pool Size=10;";
        private string  host= "localhost";
        private int port=5432;
        private string user= "postgres";
        private string psw= "postgres";
        private string dbName= "postgres";
        private int maxPoolSize = 100;

        /// <summary>
        /// 当前连接
        /// </summary>
        private NpgsqlConnection Connection = null;

        private readonly static PGLHelper instance = new PGLHelper();
        private readonly static ConcurrentDictionary<int, IDbConnection> threadConnect = new ConcurrentDictionary<int, IDbConnection>();

        public static PGLHelper Instance
        {
            get { threadConnect[Thread.CurrentThread.ManagedThreadId] = null ; return instance; }
        }


        /// <summary>
        /// 最大数据量
        /// </summary>
        private int MaximumPoolSize
        {
            get { return maxPoolSize; }
            set { maxPoolSize = value; }
        }

        /// <summary>
        /// 连接字符串
        /// </summary>
        private string ConnectStr
        {
            get {
                if (string.IsNullOrWhiteSpace(ConnectString)){
                    ConnectString = string.Format("Host={0};Port={1};Username={2};Password={3};Database={4};Pooling={5};Maximum Pool Size={6}", host, port, user, psw, dbName, IsPool, maxPoolSize);

                }
                return ConnectString;
            }
        }

        /// <summary>
        /// 是否使用连接池
        /// </summary>
        public bool IsPool { get; set; }
        private NpgsqlConnection Current { get; set; }
        public string Host { get { return host; } set { host = value; } }

        public int Port { get { return port; } set { port = value; } }

        public string UserName { get { return user; } set { user = value; } }

        public string Password { get { return psw; } set { psw = value; } }

        public string DBName { get { return dbName; } set { dbName = value; } }

        public override void Close()
        {
            if(null!=Current)
            {
                Current.Close();
            }
            IDbConnection db = null;
            if(threadConnect.TryRemove(Thread.CurrentThread.ManagedThreadId,out db))
            {
                db.Close();
                db.Dispose();
            }
        }

        public override void ClosePool()
        {
            NpgsqlConnection.ClearAllPools();
        }

        public override IDbCommand CreateCommand()
        {
            NpgsqlCommand command = new NpgsqlCommand();
            return command;
        }
        public override int ExecuteUpdate(string sql, bool scalar = false)
        {
            IDbCommand command = NewCommand(sql);
            int r = -1;
            if(scalar)
            {
               r=(int) command.ExecuteScalar();
            }
            else
            {
                r = command.ExecuteNonQuery();
            }
            return r;
        }

        public override IDbDataParameter GetDataParameter()
        {
            return new NpgsqlParameter();
        }

        public override IDataReader GetDataReader(IDbConnection connection, string sql)
        {
            IDbCommand command=  connection.CreateCommand();
            command.CommandText = sql;
           return  command.ExecuteReader();
        }

        public override IDataReader GetDataReader(string sql)
        {
            return GetDataReader(NewConnect(), sql);
        }
        public override IDbTransaction GetDbTransaction(IDbConnection con)
        {
           return con.BeginTransaction();
        }
        public override DataSet GetSelect(string sql)
        {
            DataSet ds = new DataSet();
            using (NpgsqlDataAdapter adp = new NpgsqlDataAdapter(sql,(NpgsqlConnection) NewConnect()))
            {
                adp.Fill(ds);
            }
            return ds;
        }

        public override IDbTransaction GetTransaction()
        {
           return NewConnect().BeginTransaction();
        }

      
        public override IDbCommand NewCommand(IDbConnection connection)
        {
           return connection.CreateCommand();
        }

        public override IDbCommand NewCommand(IDbConnection connection, string sql)
        {
            IDbCommand command = NewCommand(connection);
            command.CommandText = sql;
            return command;
        }

        public override IDbCommand NewCommand(string sql)
        {
            return NewCommand(NewConnect(), sql);
        }

        public override IDbConnection NewConnect()
        {
            NpgsqlConnection con = new NpgsqlConnection(ConnectStr);
            con.Open();
            Connection = con;
            int curThread = Thread.CurrentThread.ManagedThreadId;
            IDbConnection cur = null;
            if(threadConnect.ContainsKey(curThread))
            {
                //采用单例操作时
                if (threadConnect.TryRemove(curThread, out cur))
                    {
                    cur.Close();
                    cur.Dispose();
                }
                threadConnect[curThread] = con;
            }
            return con;

        }

        private  NpgsqlParameter[] CreateParameters(Dictionary<string, DBParameter> paramObj)
        {
            List<NpgsqlParameter> list = new List<NpgsqlParameter>();
            foreach(KeyValuePair<string, DBParameter> kv in paramObj)
            {
                NpgsqlParameter npgsql =(NpgsqlParameter) GetDataParameter();
                npgsql.ParameterName = kv.Key.Trim().StartsWith("@") ? kv.Key : "@" + kv.Key;
                npgsql.Value = kv.Value;
                npgsql.DbType = EnumConvert.ToEnum<DbType>(kv.Value.DbType);
                npgsql.Direction = EnumConvert.ToEnum<ParameterDirection>(kv.Value.ParameterDirection);
                list.Add(npgsql);
                
            }
            return list.ToArray();
        }


        public override int ExecuteUpdateWithParameter(string sql, bool scalar = false, Dictionary<string, DBParameter> param = null)
        {
            if(param==null||param.Count==0)
            {
              return  ExecuteUpdate(sql, scalar);
            }
            else
            {
                int r = -1;
                NpgsqlCommand command =(NpgsqlCommand) this.NewCommand(sql);
                command.Parameters.AddRange(CreateParameters(param));
                if(scalar)
                {
                    r=(int)command.ExecuteScalar();
                }
                else
                {
                    r = command.ExecuteNonQuery();
                }
                return r;
            }
        }

        public override IDataReader GetDataReaderWithParameter(IDbConnection connection, string sql, Dictionary<string, DBParameter> param = null)
        {
            if (param == null || param.Count == 0)
            {
                return this.GetDataReader(connection, sql);
            }
            else
            {

                NpgsqlCommand command = (NpgsqlCommand)this.NewCommand(connection, sql);
                command.Parameters.AddRange(CreateParameters(param));
                return command.ExecuteReader();
            }
        }

        public override IDataReader GetDataReaderWithParameter(string sql, Dictionary<string, DBParameter> param = null)
        {
            if (param == null || param.Count == 0)
            {
                return this.GetDataReader(sql);
            }
            else
            {

                NpgsqlCommand command = (NpgsqlCommand)this.NewCommandWithParameter(sql,param);
                return command.ExecuteReader();
            }
        }

        public override DataSet GetSelectWithParameter(string sql, Dictionary<string, DBParameter> param = null)
        {
            if (param == null || param.Count == 0)
            {
                return this.GetSelect(sql);
            }
            else
            {

                NpgsqlCommand command = (NpgsqlCommand)this.NewCommandWithParameter(sql,param);
               
                DataSet ds = new DataSet();
                using (NpgsqlDataAdapter adp = new NpgsqlDataAdapter(command))
                {
                    adp.Fill(ds);
                }
                return ds;
            }
        }

        public override IDbCommand NewCommandWithParameter(string sql, Dictionary<string, DBParameter> param=null)
        {
            if (param == null || param.Count == 0)
            {
                return this.NewCommand(sql);
            }
            else
            {

                NpgsqlCommand command = (NpgsqlCommand)this.NewCommand(sql);
                command.Parameters.AddRange(CreateParameters(param));
                return command;
            }
        }

    }
}
