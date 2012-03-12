using System.IO;
using System;

namespace Mono.Data.Sqlite.Orm.Tests
{
    public class OrmTestSession : SqliteSession
    {
        public OrmTestSession() 
            : base("Data Source=" + Path.GetTempFileName() + ";DefaultTimeout=100")
        {
            Trace = true;
			Console.WriteLine(Connection.ConnectionString);
        }
    }
}