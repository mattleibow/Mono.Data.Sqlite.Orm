using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
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
            this.Connection = new SqliteConnection(this.ConnectionString);

            this.QueryCache = new QueryCache(this.Connection);
            
            if (autoOpen)
            {
                this.Open();
            }
        }

        public override void Open()
        {
            this.Connection.Open();
#if NETFX_CORE
            SetTemporaryFilesDirectory(this, Windows.Storage.ApplicationData.Current.TemporaryFolder.Path);
#endif
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
        /// Get the member value.
        /// </summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <param name="member">The member to evaluate.</param>
        /// <param name="obj">The object on which to perform the evaluation.</param>
        /// <returns>The value from the expression.</returns>
        public override object GetExpressionMemberValue(Expression expression, MemberExpression member, object obj)
        {
            var propertyInfo = member.Member as PropertyInfo;
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(obj, null);
            }
            
            var fieldInfo = member.Member as FieldInfo;
            if (fieldInfo != null)
            {
#if SILVERLIGHT
                return Expression.Lambda(expression).Compile().DynamicInvoke();
#else
                return fieldInfo.GetValue(obj);
#endif
            }
            
            throw new NotSupportedException("Member Expression: " + member.Member.GetType().Name);
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

#if NETFX_CORE || SILVERLIGHT || WINDOWS_PHONE
        public enum DirectoryType : int
        {
            Data = 1,
            Temp = 2
        }

        [DllImport("sqlite3", EntryPoint = "sqlite3_win32_set_directory", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern int SetDirectory(DirectoryType directoryType, string directoryPath);

        public static void SetTemporaryFilesDirectory(SqliteSessionBase session, string directoryPath)
        {
#if NETFX_CORE
            SetDirectory(DirectoryType.Temp, directoryPath);
#elif SILVERLIGHT || WINDOWS_PHONE
            session.Execute("PRAGMA temp_store_directory = ?;", directoryPath);
#else
#endif
        }
#endif

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