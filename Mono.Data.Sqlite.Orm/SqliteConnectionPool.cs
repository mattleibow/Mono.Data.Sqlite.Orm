using System.Collections.Generic;

namespace Mono.Data.Sqlite.Orm
{
    public class SqliteConnectionPool 
    {
        private static readonly SqliteConnectionPool _shared;

        private readonly Dictionary<string, SqliteSession> _entries;

        private readonly object _entriesLock;

        static SqliteConnectionPool()
        {
            _shared = new SqliteConnectionPool();
        }

        public SqliteConnectionPool()
        {
            this._entriesLock = new object();
            this._entries = new Dictionary<string, SqliteSession>();
        }

        #region Public Properties

        /// <summary>
        ///   Gets the singleton instance of the connection tool.
        /// </summary>
        public static SqliteConnectionPool Shared
        {
            get { return _shared; }
        }

        #endregion

        #region Public Methods and Operators

        public SqliteSession GetConnection(string connectionString)
        {
            lock (this._entriesLock)
            {
                if (!this._entries.ContainsKey(connectionString))
                {
                    this._entries.Add(connectionString, new SqliteSession(connectionString));
                }

                return this._entries[connectionString];
            }
        }

        /// <summary>
        ///   Closes all connections managed by this pool.
        /// </summary>
        public void Reset()
        {
            lock (this._entriesLock)
            {
                foreach (SqliteSession entry in this._entries.Values)
                {
                    entry.Dispose();
                }

                this._entries.Clear();
            }
        }

        #endregion
    }
}