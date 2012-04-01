using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mono.Data.Sqlite.Orm.ComponentModel;

#if WINDOWS_PHONE || SILVERLIGHT
using Community.CsharpSqlite.SQLiteClient;
#endif

namespace Mono.Data.Sqlite.Orm
{
    /// <summary>
    /// 	Represents an open connection to a SQLite database.
    /// </summary>
    public class SqliteSession : IDisposable
    {
        /// <summary>
        /// 	Used to list some code that we want the MonoTouch linker
        /// 	to see, but that we never want to actually execute.
        /// </summary>
        private static readonly bool PreserveDuringLinkMagic = true;

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
        /// 	Constructs a new SqliteSession and opens a SQLite database specified by databasePath.
        /// </summary>
        /// <param name = "databasePath">
        /// 	Specifies the path to the database file.
        /// </param>
        public SqliteSession(string databasePath)
        {
            DatabasePath = databasePath;

            Connection = new SqliteConnection(DatabasePath);
            Connection.Open();
        }

        public static bool Trace { get; set; }

        [Conditional("DEBUG")]
        private static void TraceCommand(IDbCommand command)
        {
            if (Trace)
            {
                Console.WriteLine("-- Query --");
                Console.WriteLine(command.CommandText);

                Console.WriteLine("-- Arguments --");
                foreach (IDataParameter p in command.Parameters)
                {
                    Console.WriteLine(p.Value);
                }

                Console.WriteLine("-- End --");
                Console.WriteLine();
            }
        }

        public DbTransaction Transaction { get; private set; }
        public DbConnection Connection { get; private set; }
        public string DatabasePath { get; private set; }
        
        /// <summary>
        /// 	Whether <see cref = "BeginTransaction" /> has been called and the database is waiting for a <see cref = "Commit" />.
        /// </summary>
        //public bool IsInTransaction { get; private set; }

        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 	Retrieves the mapping that is automatically generated for the given type.
        /// </summary>
        /// <typeparam name = "T">
        /// 	The type whose mapping to the database is returned.
        /// </typeparam>
        /// <returns>
        /// 	The mapping represents the schema of the columns of the database and contains 
        /// 	methods to set and get properties of objects.
        /// </returns>
        public TableMapping GetMapping<T>()
        {
            Type type = typeof (T);
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

        private void CreateTable(TableMapping map)
        {
            int count = 0;

            bool exists = Table<SqliteMasterTable>().Where(t => t.Name == map.TableName).Take(1).Any();

            if (map.OldTableName != map.TableName && !string.IsNullOrEmpty(map.OldTableName))
            {
                bool oldExists = Table<SqliteMasterTable>().Where(t => t.Name == map.OldTableName).Take(1).Any();
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
                Console.WriteLine("Updates to the database: {0}", count);
            }
        }

        /// <summary>
        /// 	Executes a "create table if not exists" on the database. It also
        /// 	creates any specified indexes on the columns of the table. It uses
        /// 	a schema automatically generated from the specified type. You can
        /// 	later access this schema by calling GetMapping.
        /// </summary>
        /// <returns>
        /// 	The number of entries added to the database schema.
        /// </returns>
        public void CreateTable<T>()
        {
            // todo - allow index clearing/re-creating

            TableMapping map = GetMapping<T>();

            RunInTransaction(() => CreateTable(map));
        }

        private int MigrateTable(TableMapping map)
        {
            string query = string.Format(CultureInfo.InvariantCulture, "PRAGMA table_info([{0}]);", map.TableName);

            List<TableInfo> existingCols = Query<TableInfo>(query);

            var toBeAdded = map.Columns.Where(p => existingCols.All(e => e.Name != p.Name)).ToList();

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
        /// 	Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        /// 	in the command text for each of the arguments.
        /// </summary>
        /// <param name = "cmdText">
        /// 	The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        /// 	Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>
        /// 	A <see cref = "DbCommand" />
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand CreateCommand(string cmdText, params object[] args)
        {
            DbCommand cmd = Connection.CreateCommand();
            cmd.CommandText = cmdText;
            
            AddCommandParameters(cmd, args);

            return cmd;
        }

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
        /// 	Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// 	in the command text for each of the arguments and then executes that command.
        /// 	Use this method instead of Query when you don't expect rows back. Such cases include
        /// 	INSERTs, UPDATEs, and DELETEs.
        /// 	You can set the Trace or TimeExecution properties of the connection
        /// 	to profile execution.
        /// </summary>
        /// <param name = "query">
        /// 	The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        /// 	Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// 	The number of rows modified in the database as a result of this execution.
        /// </returns>
        public int Execute(string query, params object[] args)
        {
            using (var cmd = CreateCommand(query, args))
            {
                TraceCommand(cmd);

                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 	Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// 	in the command text for each of the arguments and then executes that command.
        /// 	It returns each row of the result using the mapping automatically generated for
        /// 	the given type.
        /// </summary>
        /// <param name = "queryText">
        /// 	The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        /// 	Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// 	An enumerable with one result for each row returned by the query.
        /// </returns>
        public List<T> Query<T>(string queryText, params object[] args) where T : new()
        {
            return DeferredQuery<T>(queryText, args).ToList();
        }

        /// <summary>
        /// 	Creates a SQLiteCommand given the command text (SQL) with arguments. Place a '?'
        /// 	in the command text for each of the arguments and then executes that command.
        /// 	It returns each row of the result using the mapping automatically generated for
        /// 	the given type.
        /// </summary>
        /// <param name = "query">
        /// 	The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        /// 	Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        /// 	An enumerable with one result for each row returned by the query.
        /// 	The enumerator will call sqlite3_step on each call to MoveNext, so the database
        /// 	connection must remain open for the lifetime of the enumerator.
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

					yield return (T) obj;
				}
			}
		}

        /// <summary>
        /// 	Executes the query and returns the value in the first column of the first row.
        /// 	All other fields are ignored.
        /// </summary>
        /// <param name = "cmdText">
        /// 	The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        /// 	Arguments to substitute for the occurences of '?' in the command text.
        /// </param>
        /// <returns>The value in the first column of the first row.</returns>
        public T ExecuteScalar<T>(string cmdText, params object[] args)
        {
            using (var cmd = CreateCommand(cmdText, args))
            {
                return ExecuteScalar<T>(cmd);
            }
        }

        public T ExecuteScalar<T>(DbCommand command)
        {
            object scalar = command.ExecuteScalar();
            return (T) Convert.ChangeType(scalar, typeof (T), CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// 	Returns a queryable interface to the table represented by the given type.
        /// </summary>
        /// <returns>
        /// 	A queryable object that is able to translate Where, OrderBy, and Take
        /// 	queries into native SQL.
        /// </returns>
        public TableQuery<T> Table<T>() where T : new()
        {
            return new TableQuery<T>(this);
        }

        /// <summary>
        /// 	Retrieves the objects matching the given primary key(s) from the table
        /// 	associated with the specified type. Use of this method requires that
        /// 	the given type has one or more designated PrimaryKey(s) (using the
        /// 	PrimaryKeyAttribute or PrimaryKeyNamesAttribute).
        /// </summary>
        /// <param name = "pk">The primary key for 'T'.</param>
        /// <param name = "pks">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The list of objects with the given primary key(s).</returns>
        public IList<T> GetList<T>(object pk, params object[] pks) where T : new()
        {
            TableMapping map = GetMapping<T>();

            var columns = map.PrimaryKeys.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray();
            var query = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM [{0}] WHERE {1}", map.TableName, string.Join(" AND ", columns));

            return Query<T>(query, new[] {pk}.Concat(pks).ToArray());
        }

        /// <summary>
        /// 	Attempts to retrieve an object with the given primary key from the table
        /// 	associated with the specified type. Use of this method requires that
        /// 	the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name = "primaryKey">The primary key for 'T'.</param>
        /// <param name = "primaryKeys">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The object with the given primary key. Throws a not found exception if the object is not found.</returns>
        public T Get<T>(object primaryKey, params object[] primaryKeys) where T : new()
        {
            return GetList<T>(primaryKey, primaryKeys).First();
        }

        /// <summary>
        /// 	Begins a new transaction. Call <see cref = "Commit" /> to end the transaction.
        /// </summary>
        public void BeginTransaction()
        {
            if (Transaction == null)
            {
                Transaction = Connection.BeginTransaction();
            }
        }

        /// <summary>
        /// 	Rolls back the transaction that was begun by <see cref = "BeginTransaction" />.
        /// </summary>
        public void Rollback()
        {
            if (Transaction != null)
            {
                Transaction.Rollback();
                Transaction = null;
            }
        }

        /// <summary>
        /// 	Commits the transaction that was begun by <see cref = "BeginTransaction" />.
        /// </summary>
        public void Commit()
        {
            if (Transaction!=null)
            {
                Transaction.Commit();
                Transaction = null;
            }
        }

        /// <summary>
        /// 	Executes <paramref name = "action" /> within a transaction and automatically rollsback the transaction       
        /// 	if an exception occurs. The exception is rethrown.
        /// </summary>
        /// <param name = "action">
        /// 	The <see cref = "Action" /> to perform within a transaction. <paramref name = "action" /> can contain 
        /// 	any number of operations on the connection but should never call <see cref = "BeginTransaction" />,
        /// 	<see cref = "Rollback" />, or <see cref = "Commit" />.
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
        public T RunInTransaction<T>(Func<T> action)
        {
            T result;
            try
            {
                BeginTransaction();
                result = action();
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
        /// 	Inserts all specified objects.
        /// </summary>
        /// <param name = "objects">
        /// 	An <see cref = "IEnumerable" /> of the objects to insert.
        /// </param>
        /// <returns>
        /// 	The number of rows added to the table.
        /// </returns>
        public int InsertAll<T>(IEnumerable<T> objects)
        {
            int result = RunInTransaction(() => objects.Sum(r => Insert(r)));
            return result;
        }

        /// <summary>
        /// 	Inserts the given object and retrieves its
        /// 	auto incremented primary key if it has one.
        /// </summary>
        /// <param name = "obj">
        /// 	The object to insert.
        /// </param>
        /// <returns>
        /// 	The number of rows added to the table.
        /// </returns>
        public int Insert<T>(T obj)
        {
            return Insert(obj, ConflictResolution.Default);
        }

        /// <summary>
        /// 	Inserts the given object and retrieves its
        /// 	auto incremented primary key if it has one.
        /// </summary>
        /// <param name = "obj">
        /// 	The object to insert.
        /// </param>
        /// <param name = "extra">
        /// 	Literal SQL code that gets placed into the command. INSERT {extra} INTO ...
        /// </param>
        /// <returns>
        /// 	The number of rows added to the table.
        /// </returns>
        public int Insert<T>(T obj, ConflictResolution extra)
        {
            TableMapping map = GetMapping<T>();
            object[] args = map.EditableColumns.Select(x => x.GetValue(obj)).ToArray();

            var insertCmd = map.GetInsertCommand(Connection, extra);
            AddCommandParameters(insertCmd, args);

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

        private DbCommand _lastInsertRowIdCommand;
        private long GetLastInsertRowId()
        {
            if (_lastInsertRowIdCommand == null)
            {
                _lastInsertRowIdCommand = Connection.CreateCommand();
                _lastInsertRowIdCommand.CommandText = "SELECT last_insert_rowid() as Id";
            }
            return (long)_lastInsertRowIdCommand.ExecuteScalar();
        }

        /// <summary>
        /// 	Updates all of the columns of a table using the specified object
        /// 	except for its primary key(s).
        /// 	The object is required to have at least one primary key.
        /// </summary>
        /// <param name = "obj">
        /// 	The object to update. It must have one or more primary keys designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// 	The number of rows updated.
        /// </returns>
        public int Update<T>(T obj)
        {
            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            var args = new List<object>();
            var sql = GetMapping<T>().GetUpdateSql(obj, args);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        /// 	Updates just the field specified by the propertyName with the values passed in the propertyValue
        /// 	The type of object to update is given by T and the primary key(s) by (primaryKey, primaryKeys)
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int Update<T>(string propertyName, object propertyValue, object primaryKey, params object[] primaryKeys)
        {
            var args = new List<object>();
            var sql = GetMapping<T>().GetUpdateSql(propertyName, propertyValue, args, primaryKey, primaryKeys);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        /// 	Updates just the field specified by 'propertyName' with the
        /// 	value 'propertyValue' for all objects of type T
        /// </summary>
        /// <returns>Number of rows affected</returns>
        public int UpdateAll<T>(string propertyName, object propertyValue)
        {
            var args = new List<object>();
            var sql = GetMapping<T>().GetUpdateAllSql(propertyName, propertyValue, args);
            return Execute(sql, args.ToArray());
        }

        /// <summary>
        /// 	Deletes the given object from the database using its primary key.
        /// </summary>
        /// <param name = "obj">
        /// 	The object to delete. It must have a primary key designated using the PrimaryKeyAttribute.
        /// </param>
        /// <returns>
        /// 	The number of rows deleted.
        /// </returns>
        public int Delete<T>(T obj)
        {
            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            var args = new List<object>();
            var sql = GetMapping<T>().GetDeleteSql(obj, args);
            return Execute(sql, args.ToArray());
        }

        public void Close()
        {
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
        }

        internal IndexInfo GetIndexInfo(string indexName)
        {
            return Query<IndexInfo>(string.Format(CultureInfo.InvariantCulture, "pragma index_info({0});", indexName)).FirstOrDefault();
        }

        internal IndexList GetIndexList(string tableName)
        {
            return Query<IndexList>(string.Format(CultureInfo.InvariantCulture, "pragma index_list({0});", tableName)).FirstOrDefault();
        }

        #region Nested type: SqliteMasterTable

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

        public class IndexInfo
        {
            [Column("seqno")]
            public int IndexRank { get; set; }

            [Column("cid")]
            public int TableRank { get; set; }

            [Column("name")]
            public string ColumnName { get; set; }
        }

        public class IndexList
        {
            [Column("seq")]
            public int Index { get; set; }

            [Column("name")]
            public string IndexName { get; set; }

            [Column("unique")]
            public bool IsUnique { get; set; }
        }

        #endregion

        #region Nested type: TableInfo

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