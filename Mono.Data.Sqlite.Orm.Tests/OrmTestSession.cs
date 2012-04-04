using System.Diagnostics;
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
            Trace = true;
			Debug.WriteLine(Connection.ConnectionString);
        }
    }
}