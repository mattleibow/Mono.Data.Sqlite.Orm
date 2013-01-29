using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    /// <summary>
    ///   Represents an open connection to a SQLite database.
    /// </summary>
    public partial class SqliteSession : SqliteSessionBase
    {
        public SqliteSession(string connectionString, bool autoOpen = true)
            : base(connectionString, autoOpen)
        {
            Connection = new SqliteConnection(this.ConnectionString);
            
            if (autoOpen)
            {
                Connection.Open();
#if NETFX_CORE
                SetTemporaryFilesDirectory(this, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
#endif
            }

            this.QueryCache = new QueryCache(this.Connection);
        }

        /// <summary>
        /// Gets the current connection to the database.
        /// </summary>
        public SqliteConnection Connection { get; private set; }

        public QueryCache QueryCache { get; private set; }

        /// <summary>
        /// Write all the details about a command to the debug log.
        /// </summary>
        /// <param name="command">The command to log.</param>
        [Conditional("DEBUG")]
        private void TraceCommand(IDbCommand command)
        {
            if (Trace)
            {
                Debug.WriteLine("-- Session --");
                Debug.WriteLine(this.SessionGuid);

                Debug.WriteLine("-- Query --");
                Debug.WriteLine(command.CommandText);

                Debug.WriteLine("-- Arguments --");
                foreach (IDataParameter p in command.Parameters)
                {
                    Debug.WriteLine(p.Value);
                }

                Debug.WriteLine("-- End --");
            }
        }

        /// <summary>
        ///   Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        ///   A queryable object that is able to translate Where, OrderBy, and Take
        ///   queries into native SQL.
        /// </returns>
        public override TableQueryBase<T> Table<T>()
        {
            return new TableQuery<T>(this);
        }

        protected override TableMapping CreateTableMapping(Type type)
        {
            return new TableMapping(type);
        }

        protected override int InsertInternal(object obj, ConflictResolution extra)
        {
            TableMapping map;
            object[] args = null;
            if (obj is Type)
            {
                map = GetMapping(obj as Type);
            }
            else
            {
                map = GetMapping(obj.GetType());
                args = map.EditableColumns.Select(x => x.GetValue(obj)).ToArray();
            }

            var insertCmd = this.QueryCache.GetInsertCommand(map, extra, args);
            this.TraceCommand(insertCmd);
            return insertCmd.ExecuteNonQuery();
        }

        protected override int UpdateInternal(object obj)
        {
            var map = GetMapping(obj.GetType());

            var args = new List<object>();
            args.AddRange(map.EditableColumns.Select(c => c.GetValue(obj)));
            args.AddRange(map.PrimaryKey.Columns.Select(c => c.GetValue(obj)));

            var insertCmd = QueryCache.GetUpdateCommand(map, ConflictResolution.Default, args.ToArray());
            this.TraceCommand(insertCmd);
            return insertCmd.ExecuteNonQuery();
        }

        /// <summary>
        ///   Begins a new transaction. Call <see cref = "SqliteTransaction.Commit" /> or 
        ///   <see cref = "SqliteTransaction.Rollback" /> to end the transaction.
        /// </summary>
        public SqliteTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        /// <summary>
        /// This function does the actual calls on the database.
        /// </summary>
        protected override IEnumerable ExecuteDeferredQueryInternal(Execution execution)
        {
            var cmd = QueryCache.CreateCommand(execution.Sql, execution.Args);

            TraceCommand(cmd);

            using (var reader = cmd.ExecuteReader())
            {
                var cols = new TableMapping.Column[reader.FieldCount];

                for (int i = 0; i < cols.Length; i++)
                {
                    cols[i] = execution.Map.FindColumn(reader.GetName(i));
                }

                while (reader.Read())
                {
                    object obj = Activator.CreateInstance(execution.Map.MappedType);
                    for (int i = 0; i < cols.Length; i++)
                    {
                        if (cols[i] != null)
                        {
                            object value = reader.GetValue(i);
                            cols[i].SetValue(obj, value);
                        }
                    }

                    var tracked = obj as ITrackConnection;
                    if (tracked != null)
                    {
                        tracked.Connection = this;
                    }

                    OnInstanceCreated(new InstanceCreatedEventArgs(obj));

                    yield return obj;
                }
            }
        }
        protected override object ExecuteScalarInternal(Execution execution)
        {
            var cmd = QueryCache.CreateCommand(execution.Sql, execution.Args);
            TraceCommand(cmd);
            return cmd.ExecuteScalar();
        }
        protected override int ExecuteInternal(Execution execution)
        {
            var cmd = QueryCache.CreateCommand(execution.Sql, execution.Args);
            TraceCommand(cmd);
            return cmd.ExecuteNonQuery();
        }

        protected override TResult RunInTransaction<TResult>(Func<TResult> action, bool createTransaction)
        {
            TResult result;

            SqliteTransaction trans = null;
            if (createTransaction)
            {
                trans = this.Connection.BeginTransaction();
            }
            try
            {
                result = action();
                if (createTransaction)
                {
                    trans.Commit();
                }
            }
            catch
            {
                if (createTransaction)
                {
                    trans.Rollback();
                }

                throw;
            }
            finally
            {
                if (createTransaction)
                {
                    trans.Dispose();
                }
            }

            return result;
        }

        /// <summary>
        /// Closes the connection to the database.
        /// </summary>
        public void Close()
        {
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
        }

        public override void Dispose()
        {
            Close();

            this.QueryCache.Dispose();
            base.Dispose();
        }
    }
}