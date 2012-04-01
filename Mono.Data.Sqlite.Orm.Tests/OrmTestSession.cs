using System.IO;
using System;

namespace Mono.Data.Sqlite.Orm.Tests
{
    public class OrmTestSession : SqliteSession
    {
        public OrmTestSession() 
#if SILVERLIGHT || WINDOWS_PHONE
            : base("Data Source=Some" + DateTime.Now.ToFileTime() + ".db;DefaultTimeout=100")
#else
            : base("Data Source=" + Path.GetTempFileName() + ";DefaultTimeout=100")
#endif
        {
#if !SILVERLIGHT && !WINDOWS_PHONE
            Trace = true;
			Console.WriteLine(Connection.ConnectionString);
#endif
        }
    }
}