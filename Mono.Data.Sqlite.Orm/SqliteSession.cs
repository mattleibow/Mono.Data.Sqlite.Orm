using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        }

        /// <summary>
        /// Gets the current connection to the database.
        /// </summary>
        public SqliteConnection Connection { get; private set; }

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

        protected override int InsertInternal(object[] args, ConflictResolution extra, TableMapping map)
        {
            var insertCmd = this.CreateCommand(map.GetInsertSql(extra, false));
            AddCommandParameters(insertCmd, args);

            this.TraceCommand(insertCmd);

            return insertCmd.ExecuteNonQuery();
        }

        /// <summary>
        ///   Inserts a record in the table with the specified defaults as the column values.
        /// </summary>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public override int InsertDefaults<T>(ConflictResolution extra)
        {
            var mapping = this.GetMapping<T>();
            var insertCmd = this.CreateCommand(mapping.GetInsertSql(extra, true));
            TraceCommand(insertCmd);
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
        ///   Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        ///   in the command text for each of the arguments.
        /// </summary>
        /// <param name = "cmdText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurrences of '?' in the command text.
        /// </param>
        /// <returns>
        ///   A <see cref = "DbCommand" />
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private DbCommand CreateCommand(string cmdText, params object[] args)
        {
            DbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = cmdText;

            AddCommandParameters(cmd, args);

            return cmd;
        }

        /// <summary>
        /// Add the specified arguments to the specified command.
        /// </summary>
        /// <param name="cmd">
        /// The command that will receive the arguments.
        /// </param>
        /// <param name="args">The arguments to add.</param>
        private static void AddCommandParameters(DbCommand cmd, params object[] args)
        {
            if (args != null)
            {
                int count = cmd.Parameters.Count;

                for (int i = 0; i < args.Length; i++)
                {
                    object value = args[i];

                    if (value != null)
                    {
                        if (value is Guid)
                        {
                            value = value.ToString();
                        }
                    }

                    if (count > i)
                    {
                        cmd.Parameters[i].Value = value;
                    }
                    else
                    {
                        DbParameter param = cmd.CreateParameter();
                        param.Value = value;
                        cmd.Parameters.Add(param);
                    }
                }
            }
        }

        /// <summary>
        /// This function does the actual calls on the database.
        /// </summary>
        protected override IEnumerable ExecuteDeferredQueryInternal(Execution execution)
        {
            var cmd = this.CreateCommand(execution.Sql, execution.Args);

            TraceCommand(cmd);

            using (DbDataReader reader = cmd.ExecuteReader())
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
            var cmd = this.CreateCommand(execution.Sql, execution.Args);
            TraceCommand(cmd);
            return cmd.ExecuteScalar();
        }
        protected override int ExecuteInternal(Execution execution)
        {
            var cmd = this.CreateCommand(execution.Sql, execution.Args);
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

            base.Dispose();
        }
    }
}