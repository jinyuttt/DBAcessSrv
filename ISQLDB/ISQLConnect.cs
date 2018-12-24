using System.Data;
namespace ISQLDB
{
    public  interface ISQLConnect
    {
       string ConnectString { get; set; }
        IDbConnection NewConnect();

        //IDbConnection GetConnectionPool();

        IDbCommand CreateCommand();

        IDbDataAdapter CreateDataAdapter();

        IDbCommand NewCommand(IDbConnection connection);

        IDbCommand NewCommand(IDbConnection connection, string sql);

        DataSet GetSelect(string sql);

        IDataReader GetDataReader(IDbConnection connection, string sql);

        void InitPool();

      
    }
}
