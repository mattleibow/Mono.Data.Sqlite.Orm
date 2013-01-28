using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    /// <summary>
    ///   Represents an open connection to a SQLite database.
    /// </summary>
    public partial class SqliteSession : SqliteSessionBase
    {
        private DbCommand _lastInsertRowIdCommand;

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

        /// <summary>
        ///   Inserts the given object and retrieves its
        ///   auto incremented primary key if it has one.
        /// </summary>
        /// <param name = "obj">
        ///   The object to insert.
        /// </param>
        /// <param name = "extra">
        ///   Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public override int Insert(object obj, ConflictResolution extra)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj", "Cannot insert a null object.");
            }

            TableMapping map = GetMapping(obj.GetType()) as TableMapping;

            DbCommand insertCmd = map.GetInsertCommand(Connection, extra, false);

            var args = map.EditableColumns.Select(x => x.GetValue(obj));

            AddCommandParameters(insertCmd, args.ToArray());

            TraceCommand(insertCmd);

            int count = insertCmd.ExecuteNonQuery();

            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            if (map.AutoIncrementColumn != null)
            {
                map.AutoIncrementColumn.SetValue(obj, GetLastInsertRowId());
            }

            return count;
        }

        /// <summary>
        ///   Inserts a record in the table with the specified defaults as the column values.
        /// </summary>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public override int InsertDefaults<T>(ConflictResolution extra)
        {
            var mapping = GetMapping<T>() as TableMapping;
            DbCommand insertCmd = mapping.GetInsertCommand(Connection, extra, true);
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

        protected override TableMappingBase CreateTableMapping(Type type)
        {
            return new TableMapping(type);
        }

        /// <summary>
        ///   Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        ///   in the command text for each of the arguments.
        /// </summary>
        /// <param name = "cmdText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        ///   A <see cref = "DbCommand" />
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand CreateCommand(string cmdText, params object[] args)
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
        /// The command that will recieve the arguments.
        /// </param>
        /// <param name="args">The arguments to add.</param>
        protected static void AddCommandParameters(DbCommand cmd, params object[] args)
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
        ///   Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///   in the command text for each of the arguments and then executes that command.
        ///   Use this method instead of Query when you don't expect rows back. Such cases include
        ///   INSERTs, UPDATEs, and DELETEs.
        ///   You can set the Trace or TimeExecution properties of the connection
        ///   to profile execution.
        /// </summary>
        /// <param name = "query">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///   The number of rows modified in the database as a result of this execution.
        /// </returns>
        public override int Execute(string query, params object[] args)
        {
            using (DbCommand cmd = CreateCommand(query, args))
            {
                TraceCommand(cmd);

                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        ///   Executes the query and returns the value in the first column of the first row.
        ///   All other fields are ignored.
        /// </summary>
        /// <param name = "cmdText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>The value in the first column of the first row.</returns>
        public override T ExecuteScalar<T>(string cmdText, params object[] args)
        {
            using (DbCommand cmd = CreateCommand(cmdText, args))
            {
                return ExecuteScalar<T>(cmd);
            }
        }

        /// <summary>
        ///   Executes the command and returns the value in the first column of the first row.
        ///   All other fields are ignored.
        /// </summary>
        /// <param name = "command">
        ///   The database commnd that contains the sql and the arguments
        /// </param>
        /// <returns>The value in the first column of the first row.</returns>
        public T ExecuteScalar<T>(DbCommand command)
        {
            object scalar = ExecuteScalar(command);
            return (T)Convert.ChangeType(scalar, typeof(T), CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///   Executes the query and returns the value in the first column of the first row.
        ///   All other fields are ignored.
        /// </summary>
        /// <param name = "cmdText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>The value in the first column of the first row.</returns>
        public override object ExecuteScalar(string cmdText, params object[] args)
        {
            using (DbCommand cmd = CreateCommand(cmdText, args))
            {
                return ExecuteScalar(cmd);
            }
        }

        /// <summary>
        ///   Executes the command and returns the value in the first column of the first row.
        ///   All other fields are ignored.
        /// </summary>
        /// <param name = "command">
        ///   The database commnd that contains the sql and the arguments
        /// </param>
        /// <returns>The value in the first column of the first row.</returns>
        public object ExecuteScalar(DbCommand command)
        {
            TraceCommand(command);
            return command.ExecuteScalar();
        }

        /// <summary>
        ///   Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///   in the command text for each of the arguments and then executes that command.
        ///   It returns each row of the result using the mapping automatically generated for
        ///   the given type.
        /// </summary>
        /// <param name = "query">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///   An enumerable with one result for each row returned by the query.
        ///   The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///   connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public override  IEnumerable<T> DeferredQuery<T>(string query, params object[] args)
        {
            DbCommand cmd = CreateCommand(query, args);
            return ExecuteDeferredQuery<T>(GetMapping<T>(), cmd);
        }

        /// <summary>
        ///   Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///   in the command text for each of the arguments and then executes that command.
        ///   It returns each row of the result using the mapping automatically generated for
        ///   the given type.
        /// </summary>
        /// <param name="type">
        ///   The type of object to return
        /// </param>
        /// <param name = "query">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///   An enumerable with one result for each row returned by the query.
        ///   The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///   connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public override  IEnumerable DeferredQuery(Type type, string query, params object[] args)
        {
            DbCommand cmd = CreateCommand(query, args);
            return ExecuteDeferredQuery(GetMapping(type), cmd);
        }

        internal List<T> ExecuteQuery<T>(TableMappingBase map, DbCommand cmd)
        {
            return ExecuteDeferredQuery<T>(map, cmd).ToList();
        }

        internal IList ExecuteQuery(TableMappingBase map, DbCommand cmd)
        {
            return ExecuteDeferredQuery(map, cmd).Cast<object>().ToList();
        }

        internal IEnumerable<T> ExecuteDeferredQuery<T>(TableMappingBase map, DbCommand cmd)
        {
            return from object result in this.ExecuteDeferredQuery(map, cmd)
                   select (T)result;
        }

        /// <summary>
        ///   Retrieves the objects matching the given primary key(s) from the table
        ///   associated with the specified type. Use of this method requires that
        ///   the given type has one or more designated PrimaryKey(s) (using the
        ///   PrimaryKeyAttribute or PrimaryKeyNamesAttribute).
        /// </summary>
        /// <param name="type">The type of object to return.</param>
        /// <param name = "pk">The primary key for 'type'.</param>
        /// <param name = "pks">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The list of objects with the given primary key(s).</returns>
        public override IList GetList(Type type, object pk, params object[] pks)
        {
            TableMapping map = GetMapping(type) as TableMapping;

            if (map.PrimaryKey == null)
            {
                throw new SqliteException("There are no primary keys");
            }

            DbCommand selectCommand = map.GetSelectCommand(this.Connection);
            AddCommandParameters(selectCommand, new[] { pk }.Concat(pks).ToArray());

            return this.ExecuteQuery(map, selectCommand);
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
        ///   Executes a "create table if not exists" on the database. It also
        ///   creates any specified indexes on the columns of the table. It uses
        ///   a schema automatically generated from the specified type. You can
        ///   later access this schema by calling GetMapping.
        /// </summary>
        /// <typeparam name="T">The table to create.</typeparam>
        /// <param name="createIndexes"> 
        ///   False if you don't want to automatically create indexes.
        /// </param>
        /// <returns>
        ///   The number of entries added to the database schema.
        /// </returns>
        public override int CreateTable<T>(bool createIndexes = true)
        {
            // todo - allow index clearing/re-creating

            using (var trans = this.Connection.BeginTransaction())
            {
                var result = CreateTable(GetMapping<T>(), createIndexes);
                trans.Commit();
                return result;
            }
        }

        /// <summary>
        ///   Executes a "create table if not exists" on the database. It also
        ///   creates any specified indexes on the columns of the table. It uses
        ///   a schema automatically generated from the specified type. You can
        ///   later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="type">The table to create.</param>
        /// <param name="createIndexes"> 
        ///   False if you don't want to automatically create indexes.
        /// </param>
        /// <returns>
        ///   The number of entries added to the database schema.
        /// </returns>
        public override int CreateTable(Type type, bool createIndexes = true)
        {
            using (var trans = this.Connection.BeginTransaction())
            {
                var result = CreateTable(GetMapping(type), createIndexes);
                trans.Commit();
                return result;
            }
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

        /// <summary>
        /// Returns the id of last inserted record.
        /// </summary>
        /// <returns>
        /// The autogenerated id.
        /// </returns>
        private long GetLastInsertRowId()
        {
            if (_lastInsertRowIdCommand == null)
            {
                _lastInsertRowIdCommand = Connection.CreateCommand();
                _lastInsertRowIdCommand.CommandText = "SELECT last_insert_rowid() as Id";
            }
            return (long)_lastInsertRowIdCommand.ExecuteScalar();
        }

        internal IEnumerable ExecuteDeferredQuery(TableMappingBase map, DbCommand cmd)
        {
            TraceCommand(cmd);

            using (DbDataReader reader = cmd.ExecuteReader())
            {
                var cols = new TableMappingBase.Column[reader.FieldCount];

                for (int i = 0; i < cols.Length; i++)
                {
                    cols[i] = map.FindColumn(reader.GetName(i));
                }

                while (reader.Read())
                {
                    object obj = Activator.CreateInstance(map.MappedType);
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

        public override void Dispose()
        {
            Close();

            if (_lastInsertRowIdCommand != null)
            {
                _lastInsertRowIdCommand.Dispose();
            }

            base.Dispose();
        }
    }
}