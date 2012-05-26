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
using Mono.Data.Sqlite.Orm.ComponentModel;

#if WINDOWS_PHONE || SILVERLIGHT || NETFX_CORE
using Community.CsharpSqlite.SQLiteClient;
#endif

namespace Mono.Data.Sqlite.Orm
{
    /// <summary>
    ///   Represents an open connection to a SQLite database.
    /// </summary>
    public partial class SqliteSession : IDisposable
    {
        /// <summary>
        /// The unique id of this session.
        /// </summary>
        public readonly Guid SessionGuid;

        /// <summary>
        ///   Used to list some code that we want the MonoTouch linker
        ///   to see, but that we never want to actually execute.
        /// </summary>
        private static readonly bool PreserveDuringLinkMagic = true;

        private DbCommand _lastInsertRowIdCommand;

        private Dictionary<string, TableMapping> _tables;

        static SqliteSession()
        {
            if (!PreserveDuringLinkMagic)
            {
                var info = new TableInfo {Name = "magic"};
                PreserveDuringLinkMagic = info.Name != "magic";
            }
        }

        /// <summary>
        ///   Constructs a new SqliteSession and opens a SQLite database 
        ///   specified by <paramref name="connectionString"/>.
        /// </summary>
        /// <param name = "connectionString">
        ///   Specifies the path to the database file.
        /// </param>
        public SqliteSession(string connectionString)
        {
            SessionGuid = Guid.NewGuid();

            this.ConnectionString = connectionString;

            Connection = new SqliteConnection(this.ConnectionString);
            Connection.Open();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to write all command 
        /// information to the debug log.
        /// </summary>
        public static bool Trace { get; set; }

        /// <summary>
        /// Gets the current transaction. Null if no curent transaction.
        /// </summary>
        public DbTransaction Transaction { get; private set; }

        /// <summary>
        /// Gets the current connection to the database.
        /// </summary>
        public DbConnection Connection { get; private set; }

        /// <summary>
        /// Gets the database connction string for this connection.
        /// </summary>
        public string ConnectionString { get; private set; }

        #region IDisposable Members

        /// <summary>
        ///   Whether <see cref = "BeginTransaction" /> has been called and the database is waiting for a <see cref = "Commit" />.
        /// </summary>
        //public bool IsInTransaction { get; private set; }
        public void Dispose()
        {
            Close();
        }

        #endregion

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
        ///   Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <typeparam name = "T">
        ///   The type whose mapping to the database is returned.
        /// </typeparam>
        /// <returns>
        ///   The mapping represents the schema of the columns of the database and contains 
        ///   methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping<T>()
        {
            return GetMapping(typeof (T));
        }

        /// <summary>
        ///   Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <param name = "type">
        ///   The type whose mapping to the database is returned.
        /// </param>
        /// <returns>
        ///   The mapping represents the schema of the columns of the database and contains 
        ///   methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping(Type type)
        {
            string typeFullName = type.FullName ?? string.Empty;

            if (_tables == null)
            {
                _tables = new Dictionary<string, TableMapping>();
            }
            TableMapping map;
            if (!_tables.TryGetValue(typeFullName, out map))
            {
                map = new TableMapping(type);
                _tables[typeFullName] = map;
            }
            return map;
        }

        private int CreateTable(TableMapping map)
        {
            int count = 0;

            bool exists = this.TableExists(map);

            if (map.OldTableName != map.TableName && !string.IsNullOrEmpty(map.OldTableName))
            {
                bool oldExists = this.TableExists(map.OldTableName);
                if (!oldExists)
                {
                    throw new InvalidOperationException(map.OldTableName + " does not exist.");
                }
                if (exists)
                {
                    throw new InvalidOperationException(map.TableName + " already exists.");
                }

                count += Execute(map.GetRenameSql());

                exists = true;
            }

            if (!exists)
            {
                count += Execute(map.GetCreateSql());
            }
            else
            {
                // Table already exists, migrate it
                count += MigrateTable(map);
            }

            // create indexes
            count += map.Indexes.Sum(index => Execute(index.GetCreateSql(map.TableName)));

            if (Trace)
            {
                Debug.WriteLine("Updates to the database: {0}", count);
            }

            return count;
        }

        /// <summary>
        /// Checks to see if a particular table exists.
        /// </summary>
        /// <typeparam name="T">The table to check for.</typeparam>
        /// <returns>
        /// True if the table exists in the database, otherwise False.
        /// </returns>
        public bool TableExists<T>()
        {
            return TableExists(GetMapping<T>());
        }

        /// <summary>
        /// Checks to see if a particular table exists.
        /// </summary>
        /// <param name="type">The table to check for.</param>
        /// <returns>
        /// True if the table exists in the database, otherwise False.
        /// </returns>
        public bool TableExists(Type type)
        {
            return TableExists(GetMapping(type));
        }

        /// <summary>
        /// Checks to see if a particular table exists.
        /// </summary>
        /// <param name="map">The table mapping to use.</param>
        /// <returns>
        /// True if the table exists in the database, otherwise False.
        /// </returns>
        private bool TableExists(TableMapping map)
        {
            return this.TableExists(map.TableName);
        }

        /// <summary>
        /// Checks to see if a particular table exists.
        /// </summary>
        /// <param name="tableName">The table to check for.</param>
        /// <returns>
        /// True if the table exists in the database, otherwise False.
        /// </returns>
        private bool TableExists(string tableName)
        {
            return this.Table<SqliteMasterTable>().Where(t => t.Name == tableName && t.Type == "table").Take(1).Any();
        }

        /// <summary>
        ///   Executes a "create table if not exists" on the database. It also
        ///   creates any specified indexes on the columns of the table. It uses
        ///   a schema automatically generated from the specified type. You can
        ///   later access this schema by calling GetMapping.
        /// </summary>
        /// <typeparam name="T">The table to create.</typeparam>
        /// <returns>
        ///   The number of entries added to the database schema.
        /// </returns>
        public int CreateTable<T>()
        {
            // todo - allow index clearing/re-creating

           return RunInTransaction(() => CreateTable(GetMapping<T>()));
        }

        /// <summary>
        ///   Executes a "create table if not exists" on the database. It also
        ///   creates any specified indexes on the columns of the table. It uses
        ///   a schema automatically generated from the specified type. You can
        ///   later access this schema by calling GetMapping.
        /// </summary>
        /// <param name="type">The table to create.</param>
        /// <returns>
        ///   The number of entries added to the database schema.
        /// </returns>
        public int CreateTable(Type type)
        {
            return RunInTransaction(() => CreateTable(GetMapping(type)));
        }

        private int MigrateTable(TableMapping map)
        {
            string query = string.Format(CultureInfo.InvariantCulture, "PRAGMA table_info([{0}]);", map.TableName);

            List<TableInfo> existingCols = Query<TableInfo>(query);

            List<TableMapping.Column> toBeAdded = map.Columns.Where(p => existingCols.All(e => e.Name != p.Name)).ToList();

            return toBeAdded.Sum(col =>
                                     {
                                         if (!col.IsNullable && string.IsNullOrEmpty(col.DefaultValue))
                                         {
                                             throw new InvalidOperationException("Unable to alter table. Cannot add non-nullable column: " + col.Name);
                                         }

                                         return Execute(string.Format(CultureInfo.InvariantCulture, "ALTER TABLE [{0}] ADD COLUMN {1};",
                                                                      map.TableName,
                                                                      col.GetCreateSql(map)));
                                     });
        }

        /// <summary>
        ///   Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        /// <typeparam name="T">The table to drop.</typeparam>
        public int DropTable<T>()
        {
            return DropTable(typeof (T));
        }

        /// <summary>
        ///   Executes a "drop table" on the database.  This is non-recoverable.
        /// </summary>
        /// <param name="type">The table to drop.</param>
        public int DropTable(Type type)
        {
            TableMapping map = GetMapping(type);

            string query = string.Format("DROP TABLE IF EXISTS [{0}]", map.TableName);

            int count = Execute(query);

            return count;
        }
        /// <summary>
        ///   Executes a "delete from table" on the database. This is non-recoverable.
        /// </summary>
        /// <typeparam name="T">The table to clear.</typeparam>
        public int ClearTable<T>()
        {
            return ClearTable(typeof(T));
        }

        /// <summary>
        ///   Executes a "delete from table" on the database. This is non-recoverable.
        /// </summary>
        /// <param name="type">The table to clear.</param>
        public int ClearTable(Type type)
        {
            TableMapping map = GetMapping(type);

            string query = string.Format("DELETE FROM [{0}]", map.TableName);

            int count = Execute(query);

            return count;
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
        public int Execute(string query, params object[] args)
        {
            using (DbCommand cmd = CreateCommand(query, args))
            {
                TraceCommand(cmd);

                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        ///   Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        ///   in the command text for each of the arguments and then executes that command.
        ///   It returns each row of the result using the mapping automatically generated for
        ///   the given type.
        /// </summary>
        /// <param name = "queryText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///   An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string queryText, params object[] args) where T : new()
        {
            return DeferredQuery<T>(queryText, args).ToList();
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
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args) where T : new()
        {
            DbCommand cmd = CreateCommand(query, args);
            return ExecuteDeferredQuery<T>(GetMapping<T>(), cmd);
        }

        internal List<T> ExecuteQuery<T>(TableMapping map, DbCommand cmd)
        {
            return ExecuteDeferredQuery<T>(map, cmd).ToList();
        }

        internal IEnumerable<T> ExecuteDeferredQuery<T>(TableMapping map, DbCommand cmd)
        {
            TraceCommand(cmd);

            using (DbDataReader reader = cmd.ExecuteReader())
            {
                var cols = new TableMapping.Column[reader.FieldCount];

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

                    yield return (T) obj;
                }
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
        public T ExecuteScalar<T>(string cmdText, params object[] args)
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
            object scalar = command.ExecuteScalar();
            return (T) Convert.ChangeType(scalar, typeof (T), CultureInfo.CurrentCulture);
        }

        /// <summary>
        ///   Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        ///   A queryable object that is able to translate Where, OrderBy, and Take
        ///   queries into native SQL.
        /// </returns>
        public TableQuery<T> Table<T>() where T : new()
        {
            return new TableQuery<T>(this);
        }

        /// <summary>
        ///   Retrieves the objects matching the given primary key(s) from the table
        ///   associated with the specified type. Use of this method requires that
        ///   the given type has one or more designated PrimaryKey(s) (using the
        ///   PrimaryKeyAttribute or PrimaryKeyNamesAttribute).
        /// </summary>
        /// <param name = "pk">The primary key for 'T'.</param>
        /// <param name = "pks">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The list of objects with the given primary key(s).</returns>
        public IList<T> GetList<T>(object pk, params object[] pks) where T : new()
        {
            TableMapping map = GetMapping<T>();

            string[] columns = map.PrimaryKeys.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray();
            string query = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}] WHERE {1}", map.TableName, string.Join(" AND ", columns));

            return Query<T>(query, new[] {pk}.Concat(pks).ToArray());
        }

        /// <summary>
        ///   Attempts to retrieve an object with the given primary key from the table
        ///   associated with the specified type. Use of this method requires that
        ///   the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name = "primaryKey">The primary key for 'T'.</param>
        /// <param name = "primaryKeys">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The object with the given primary key. Throws a not found exception if the object is not found.</returns>
        public T Get<T>(object primaryKey, params object[] primaryKeys) where T : new()
        {
            return GetList<T>(primaryKey, primaryKeys).First();
        }

        /// <summary>
        ///   Attempts to retrieve an object with the given LINQ exprsion from 
        ///   the table associated with the specified type.
        /// </summary>
        /// <param name = "expression">The LINQ expression to use.</param>
        /// <returns>
        ///   The object that matches the given LINQ expression. 
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   The object is not found.
        /// </exception>
        public T Get<T>(Expression<Func<T, bool>> expression) where T : new()
        {
            return Table<T>().Where(expression).First();
        }

        /// <summary>
        ///   Attempts to retrieve an object with the given primary key from the table
        ///   associated with the specified type. Use of this method requires that
        ///   the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name = "primaryKey">The primary key for 'T'.</param>
        /// <param name = "primaryKeys">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The object with the given primary key or null if the object is not found.</returns>
        public T Find<T>(object primaryKey, params object[] primaryKeys) where T : new()
        {
            return GetList<T>(primaryKey, primaryKeys).FirstOrDefault();
        }

        /// <summary>
        ///   Begins a new transaction. Call <see cref = "Commit" /> to end the transaction.
        /// </summary>
        public void BeginTransaction()
        {
            if (Transaction == null)
            {
                Debug.WriteLine("BEGIN TRANSACTION");

                Transaction = Connection.BeginTransaction();
            }
        }

        /// <summary>
        ///   Rolls back the transaction that was begun by <see cref = "BeginTransaction" />.
        /// </summary>
        public void Rollback()
        {
            if (Transaction != null)
            {
                Debug.WriteLine("ROLLBACK TRANSACTION");

                Transaction.Rollback();
                Transaction = null;
            }
        }

        /// <summary>
        ///   Commits the transaction that was begun by <see cref = "BeginTransaction" />.
        /// </summary>
        public void Commit()
        {
            if (Transaction != null)
            {
                Debug.WriteLine("COMMIT TRANSACTION");

                Transaction.Commit();
                Transaction = null;
            }
        }

        /// <summary>
        ///   Executes <paramref name = "action" /> within a transaction and automatically rollsback the transaction       
        ///   if an exception occurs. The exception is rethrown.
        /// </summary>
        /// <param name = "action">
        ///   The <see cref = "Action" /> to perform within a transaction. <paramref name = "action" /> can contain 
        ///   any number of operations on the connection but should never call <see cref = "BeginTransaction" />,
        ///   <see cref = "Rollback" />, or <see cref = "Commit" />.
        /// </param>
        public void RunInTransaction(Action action)
        {
            try
            {
                BeginTransaction();
                action();
                Commit();
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }
        }

        /// <summary>
        ///   Executes <paramref name = "function" /> within a transaction and automatically rollsback the transaction       
        ///   if an exception occurs. The exception is rethrown.
        /// </summary>
        /// <param name = "function">
        ///   The <see cref = "Action" /> to perform within a transaction. <paramref name = "function" /> can contain 
        ///   any number of operations on the connection but should never call <see cref = "BeginTransaction" />,
        ///   <see cref = "Rollback" />, or <see cref = "Commit" />.
        /// </param>
        /// <returns>
        ///   The value <paramref name="function"/> returns.
        /// </returns>
        public T RunInTransaction<T>(Func<T> function)
        {
            T result;
            try
            {
                BeginTransaction();
                result = function();
                Commit();
            }
            catch (Exception)
            {
                Rollback();
                throw;
            }

            return result;
        }

        /// <summary>
        ///   Inserts all specified objects.
        /// </summary>
        /// <param name = "objects">
        ///   An <see cref = "IEnumerable" /> of the objects to insert.
        /// </param>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public int InsertAll<T>(IEnumerable<T> objects)
        {
            int result = RunInTransaction(() => objects.Sum(r => Insert(r)));
            return result;
        }

        /// <summary>
        ///   Inserts a record in the table with the specified defaults as the column values.
        /// </summary>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public int Insert<T>()
            where T : class
        {
            return Insert<T>(null);
        }

        /// <summary>
        ///   Inserts the given object and retrieves its
        ///   auto incremented primary key if it has one.
        /// </summary>
        /// <param name = "obj">
        ///   The object to insert.
        /// </param>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public int Insert<T>(T obj)
        {
            return Insert(obj, ConflictResolution.Default);
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
        public int Insert<T>(T obj, ConflictResolution extra)
        {
            TableMapping map = GetMapping<T>();

            var instance = obj as object;

            DbCommand insertCmd = map.GetInsertCommand(Connection, extra, instance == null);

            if (instance != null)
            {
                var args = map.EditableColumns.Select(x =>
                                                          {
                                                              var value = x.GetValue(obj);
                                                              return value == null
                                                                         ? null
                                                                         : Convert.ChangeType(value, x.ColumnType, CultureInfo.InvariantCulture);
                                                          });
                AddCommandParameters(insertCmd, args.ToArray());
            }

            TraceCommand(insertCmd);

            int count = insertCmd.ExecuteNonQuery();

            if (instance != null)
            {
                var tracked = obj as ITrackConnection;
                if (tracked != null)
                {
                    tracked.Connection = this;
                }

                if (map.AutoIncrementColumn != null)
                {
                    map.AutoIncrementColumn.SetValue(obj, GetLastInsertRowId());
                }
            }

            return count;
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
            return (long) _lastInsertRowIdCommand.ExecuteScalar();
        }

        /// <summary>
        ///   Updates all of the columns of a table using the specified object
        ///   except for its primary key(s).
        ///   The object is required to have at least one primary key.
        /// </summary>
        /// <param name = "obj">
        ///   The object to update. It must have one or more primary keys designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        ///   The number of rows updated.
        /// </returns>
        public int Update<T>(T obj)
        {
            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            var args = new List<object>();
            string sql = GetMapping<T>().GetUpdateSql(obj, args);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        ///   Updates just the field specified by the propertyName with the values passed in the propertyValue
        ///   The type of object to update is given by T and the primary key(s) by (primaryKey, primaryKeys)
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int Update<T>(string propertyName, object propertyValue, object primaryKey, params object[] primaryKeys)
        {
            var args = new List<object>();
            string sql = GetMapping<T>().GetUpdateSql(propertyName, propertyValue, args, primaryKey, primaryKeys);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        ///   Updates just the field specified by 'propertyName' with the
        ///   value 'propertyValue' for all objects of type T
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int UpdateAll<T>(string propertyName, object propertyValue)
        {
            var args = new List<object>();
            string sql = GetMapping<T>().GetUpdateAllSql(propertyName, propertyValue, args);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        ///   Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name = "obj">
        ///   The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        ///   The number of rows deleted.
        /// </returns>
        public int Delete<T>(T obj)
        {
            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            var args = new List<object>();
            string sql = GetMapping<T>().GetDeleteSql(obj, args);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        /// Closes the connectiion to the database.
        /// </summary>
        public void Close()
        {
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
        }

        /// <summary>
        /// Returns relevant information for a particular index.
        /// </summary>
        /// <param name="indexName">
        /// The index name to find.
        /// </param>
        /// <returns>
        /// The nformation relting to the index.
        /// </returns>
        public IList<IndexInfo> GetIndexInfo(string indexName)
        {
            return Query<IndexInfo>(string.Format(CultureInfo.InvariantCulture, "pragma index_info({0});", indexName)).ToList();
        }

        /// <summary>
        /// Returns a list of indexes for the specified table.
        /// </summary>
        /// <param name="tableName">
        /// The table name to use.
        /// </param>
        /// <returns>
        /// The list of indexes.
        /// </returns>
        public IList<IndexListItem> GetIndexList(string tableName)
        {
            return Query<IndexListItem>(string.Format(CultureInfo.InvariantCulture, "pragma index_list({0});", tableName)).ToList();
        }

        /// <summary>
        /// This event is triggered whenever this connection is finished 
        /// creating a new object from a database record.
        /// </summary>
        public event EventHandler<InstanceCreatedEventArgs> InstanceCreated;

        /// <summary>
        /// Raises the <see cref="InstanceCreated"/> event with the specified 
        /// event arguments.
        /// </summary>
        /// <param name="e">
        /// The event arguments to send.
        /// </param>
        protected virtual void OnInstanceCreated(InstanceCreatedEventArgs e)
        {
            if (InstanceCreated != null)
            {
                InstanceCreated(this, e);
            }
        }

        #region Nested type: IndexInfo

        /// <summary>
        /// Represents an index in the database
        /// </summary>
        public class IndexInfo
        {
            [Column("seqno")]
            public int IndexRank { get; set; }

            [Column("cid")]
            public int TableRank { get; set; }

            [Column("name")]
            public string ColumnName { get; set; }
        }

        #endregion

        #region Nested type: IndexListItem

        /// <summary>
        /// Represents an index of a particular table.
        /// </summary>
        public class IndexListItem
        {
            [Column("seq")]
            public int Index { get; set; }

            [Column("name")]
            public string IndexName { get; set; }

            [Column("unique")]
            public bool IsUnique { get; set; }
        }

        #endregion

        #region Nested type: SqliteMasterTable

        /// <summary>
        /// Represents the sqlite master table
        /// </summary>
        [Table("sqlite_master")]
        public class SqliteMasterTable
        {
            public int RootPage { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }

            [Column("tbl_name")]
            public string TableName { get; set; }

            public string Sql { get; set; }
        }

        #endregion

        #region Nested type: TableInfo

        /// <summary>
        /// Represents a table in the database.
        /// </summary>
        public class TableInfo
        {
            [Column("cid")]
            public int Id { get; set; }

            public string Name { get; set; }

            [Column("type")]
            public string ObjectType { get; set; }

            public int NotNull { get; set; }

            [Column("dflt_value")]
            public string DefaultValue { get; set; }

            [Column("primaryKey")]
            public bool IsPrimaryKey { get; set; }
        }

        #endregion
    }
}