using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public static class SqliteSessionExtensions
    {
        public static Task<int> CreateTableAsync<T>(this SqliteSessionBase session, bool createIndexes = true) where T : new()
        {
            return Task<int>.Factory.StartNew(
                () =>
                    {
                        var conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.CreateTable<T>(createIndexes);
                        }
                    });
        }

        public static Task<int> DeleteAsync<T>(this SqliteSessionBase session, T item)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Delete(item);
                        }
                    });
        }

        public static Task<int> DropTableAsync<T>(this SqliteSessionBase session) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.DropTable<T>();
                        }
                    });
        }

        public static Task<int> ClearTableAsync<T>(this SqliteSessionBase session) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.ClearTable<T>();
                        }
                    });
        }

        public static Task<int> ExecuteAsync(this SqliteSessionBase session, string query, params object[] args)
        {
            return Task<int>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Execute(query, args);
                        }
                    });
        }

        public static Task<T> ExecuteScalarAsync<T>(this SqliteSessionBase session, string sql, params object[] args)
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.ExecuteScalar<T>(sql, args);
                        }
                    });
        }

        public static Task<T> GetAsync<T>(this SqliteSessionBase session, object pk, params object[] primaryKeys) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Get<T>(pk, primaryKeys);
                        }
                    });
        }

        public static Task<T> GetAsync<T>(this SqliteSessionBase session, Expression<Func<T, bool>> expression) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Get(expression);
                        }
                    });
        }

        public static Task<T> FindAsync<T>(this SqliteSessionBase session, object pk, params object[] primaryKeys) where T : new()
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Find<T>(pk, primaryKeys);
                        }
                    });
        }

        public static Task<int> InsertAllAsync<T>(this SqliteSessionBase session, IEnumerable<T> items)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.InsertAll(items);
                        }
                    });
        }

        public static Task<int> InsertAsync<T>(this SqliteSessionBase session, T item)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Insert(item);
                        }
                    });
        }

        public static Task<int> InsertAsync<T>(this SqliteSessionBase session, T item, ConflictResolution extra)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Insert(item, extra);
                        }
                    });
        }

        public static Task<int> InsertDefaultsAsync<T>(this SqliteSessionBase session) where T : class
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.InsertDefaults<T>();
                        }
                    });
        }

        public static Task<List<T>> QueryAsync<T>(this SqliteSessionBase session, string sql, params object[] args) where T : new()
        {
            return Task<List<T>>.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Query<T>(sql, args);
                        }
                    });
        }

        public static Task<int> UpdateAsync<T>(this SqliteSessionBase session, T item)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.Update(item);
                        }
                    });
        }

        public static Task<int> UpdateAllAsync<T>(this SqliteSessionBase session, string propertyName, object propertyValue)
        {
            return Task.Factory.StartNew(
                () =>
                    {
                        SqliteSession conn = GetAsyncConnection(session);
                        using (conn.Lock())
                        {
                            return conn.UpdateAll<T>(propertyName, propertyValue);
                        }
                    });
        }


        private static SqliteSession GetAsyncConnection(SqliteSessionBase session)
        {
            return SqliteConnectionPool.Shared.GetConnection(session.ConnectionString);
        }
    }
}