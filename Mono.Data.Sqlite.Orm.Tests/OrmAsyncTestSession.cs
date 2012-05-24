using System.Diagnostics;
using System.IO;

namespace Mono.Data.Sqlite.Orm.Tests
{
    public static class OrmAsyncTestSession
    {
        public static SqliteSession GetConnection(string connectionString)
        {
            SqliteSession session = SqliteConnectionPool.Shared.GetConnection(connectionString);

            SqliteSession.Trace = true;
            Debug.WriteLine(session.Connection.ConnectionString);

            return session;
        }

        public static SqliteSession GetConnection()
        {
#if SILVERLIGHT || WINDOWS_PHONE
            var path = ("Data Source=Some" + DateTime.Now.Ticks + ".db,DefaultTimeout=100");
#elif NETFX_CORE
            var path = ("Data Source=file:" + ApplicationData.Current.TemporaryFolder.Path + "\\TestDatabase" + DateTime.Now.Ticks + ".db,DefaultTimeout=100");
#else
            var path = ("Data Source=" + Path.GetTempFileName() + ";DefaultTimeout=100");
#endif

            return GetConnection(path);
        }
    }
}