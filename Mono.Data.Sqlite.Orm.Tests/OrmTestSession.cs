using System.Diagnostics;
using System.IO;
using System;

#if NETFX_CORE
using Windows.Storage;
#endif

namespace Mono.Data.Sqlite.Orm.Tests
{
    public class OrmTestSession : SqliteSession
    {
        public OrmTestSession()
#if SILVERLIGHT || WINDOWS_PHONE
            : base("Data Source=Some" + DateTime.Now.Ticks + ".db,DefaultTimeout=100")
#elif NETFX_CORE
            : base("Data Source=file:" + ApplicationData.Current.TemporaryFolder.Path + "\\TestDatabase" + DateTime.Now.Ticks + ".db,DefaultTimeout=100")
#else
            : base("Data Source=" + Path.GetTempFileName() + ";DefaultTimeout=100")
#endif
        {
            Trace = true;
            Debug.WriteLine(Connection.ConnectionString);
        }
    }
}