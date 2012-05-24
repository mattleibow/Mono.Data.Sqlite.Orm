using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public partial class SqliteSession
    {
        #region Public Methods and Operators

        public Task<int> CreateTableAsync<T>() where T : new()
        {
            return Task<int>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.CreateTable<T>();
                        }
                    });
        }

        public Task<int> DeleteAsync<T>(T item)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Delete(item);
                        }
                    });
        }

        public Task<int> DropTableAsync<T>() where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.DropTable<T>();
                        }
                    });
        }

        public Task<int> ExecuteAsync(string query, params object[] args)
        {
            return Task<int>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Execute(query, args);
                        }
                    });
        }

        public Task<T> ExecuteScalarAsync<T>(string sql, params object[] args)
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            DbCommand command = conn.CreateCommand(sql, args);
                            return (T)command.ExecuteScalar();
                        }
                    });
        }

        public Task<T> GetAsync<T>(object pk, params object[] primaryKeys) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Get<T>(pk, primaryKeys);
                        }
                    });
        }

        public Task<T> FindAsync<T>(object pk, params object[] primaryKeys) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Find<T>(pk, primaryKeys);
                        }
                    });
        }

        public Task<int> InsertAllAsync<T>(IEnumerable<T> items)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.InsertAll(items);
                        }
                    });
        }

        public Task<int> InsertAsync<T>(T item)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Insert(item);
                        }
                    });
        }

        public Task<int> InsertAsync<T>(T item, ConflictResolution extra)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Insert(item, extra);
                        }
                    });
        }

        public Task<int> InsertAsync<T>()
            where T : class
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Insert<T>(null);
                        }
                    });
        }

        public Task<List<T>> QueryAsync<T>(string sql, params object[] args) where T : new()
        {
            return Task<List<T>>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Query<T>(sql, args);
                        }
                    });
        }

        public Task RunInTransactionAsync(Action action)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            conn.RunInTransaction(action);
                        }
                    });
        }

        public Task<int> UpdateAsync<T>(T item)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.Update(item);
                        }
                    });
        }

        public Task<int> UpdateAllAsync<T>(string propertyName, object propertyValue)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = this.GetAsyncConnection();
                        using (conn.Lock())
                        {
                            return conn.UpdateAll<T>(propertyName, propertyValue);
                        }
                    });
        }

        #endregion

        #region Methods

        private SqliteSession GetAsyncConnection()
        {
            return SqliteConnectionPool.Shared.GetConnection(this.ConnectionString);
        }

        #region Public Methods and Operators

        public IDisposable Lock()
        {
            return new LockWrapper(this);
        }

        #endregion

        #region Nested type: LockWrapper

        private class LockWrapper : IDisposable
        {
            #region Constants and Fields

            private readonly object _lockPoint;

            #endregion

            #region Constructors and Destructors

            public LockWrapper(object lockPoint)
            {
                this._lockPoint = lockPoint;
                Monitor.Enter(this._lockPoint);
            }

            #endregion

            #region Public Methods and Operators

            public void Dispose()
            {
                Monitor.Exit(this._lockPoint);
            }

            #endregion
        }

        #endregion

        #endregion
    }
}