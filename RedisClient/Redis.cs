using Serializer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RedisClient
{
    public class Redis
    {
        int Default_Timeout = 600;//默认超时时间（单位秒）
        public static List<string> Address { get; set; }

        public static int PoolSize { get; set; }

        public static bool IsSSL { get; set; }

        public static string Password { get; set; }

        public static string Prefix { get; set; }

        public static readonly Redis Instance = new Redis();

        public Redis()
        {
            PoolSize = 10;
            Password = "123456";
            Prefix = "Key_DB_";
            Init();

        }
        public void Init()
        {
            StringBuilder builder = new StringBuilder();
            List<string> lstConnect = new List<string>();
            if(Address==null)
            {
                Address = new List<string>();
            }
            if(Address.Count==0)
            {
                Address.Add("127.0.0.1:6379");
            }
           foreach(string addr in Address)
            {
                builder.Clear();
                builder.AppendFormat("{0},password={1},defaultDatabase=0,poolsize={2},preheat=true,ssl={3},writeBuffer=10240,prefix={4}", addr, Password, PoolSize, IsSSL, Prefix);
                lstConnect.Add(builder.ToString());
            }
            var csredis = new CSRedis.CSRedisClient(null, lstConnect.ToArray());
            RedisHelper.Initialization(csredis,null,null);
           
  
        }

        /// <summary>
        /// 连接超时设置
        /// </summary>
        public int TimeOut
        {
            get
            {
                return Default_Timeout;
            }
            set
            {
                Default_Timeout = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Get(string key)
        {
            return Get<object>(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            
             byte[] data=  RedisHelper.GetBytes(key);
             return  SerializerFactory<CommonSerializer>.Deserialize<T>(data);
        }

        public Dictionary<object,T> GetKVS<T>(object[] keys)
        {
            Dictionary<object, T> dicResult = new Dictionary<object, T>(keys.Length);
            foreach (object key in keys)
            {
                byte[] data = RedisHelper.GetBytes(key.ToString());
                T value= SerializerFactory<CommonSerializer>.Deserialize<T>(data);
                dicResult[key]=value;
            }
            return dicResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        public void Insert(string key, object data)
        {
            RedisHelper.SetBytes(key, null);
          
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <param name="cacheTime"></param>
        public void Insert(string key, object data, int cacheTime=-1)
        {
           byte[] value= SerializerFactory<CommonSerializer>.Serializer(data);
           RedisHelper.SetBytes(key, value, cacheTime);
        }


        public async Task Insert(Dictionary<object,object> data,int cacheTime=-1)
        {
            foreach(KeyValuePair<object,object> kv in data)
            {
                byte[] value = SerializerFactory<CommonSerializer>.Serializer(kv.Value);
                await RedisHelper.SetBytesAsync(kv.Key.ToString(), value,cacheTime);
            }
          
        }

        public async Task<bool> Insert<T>(string key, T data)
        {
            byte[] value = SerializerFactory<CommonSerializer>.Serializer(data);
            return await RedisHelper.SetBytesAsync(key, value);
        }

        public async Task<bool> Insert<T>(string key, T data, int cacheTime)
        {
             byte[] value = SerializerFactory<CommonSerializer>.Serializer(data);
             return await RedisHelper.SetBytesAsync(key, value, cacheTime);
        }

        public async Task Remove(object[] keys)
        {
            foreach (object key in keys)
            {
                await RedisHelper.RemoveAsync(key.ToString());
            }

        }


        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="key"></param>
        public async Task<long> Remove(string key)
        {
          return  await  RedisHelper.RemoveAsync(key);

        }

        /// <summary>
        /// 判断key是否存在
        /// </summary>
        public async Task<bool> Exists(string key)
        {
            return await RedisHelper.ExistsAsync(key);
        }

        public async Task<string[]> GetAllKeys()
        {
            return await RedisHelper.KeysAsync("*");
        }
        public async Task Clear()
        {
            string[] keys =await GetAllKeys();
            Parallel.ForEach(keys, key =>
            {
                RedisHelper.Remove(key);
            });
        }
    }
}
