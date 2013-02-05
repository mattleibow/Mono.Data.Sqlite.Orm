using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public abstract class SqliteSessionBase : IDisposable
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

        private Dictionary<string, TableMapping> _tables;

        static SqliteSessionBase()
        {
            if (!PreserveDuringLinkMagic)
            {
                var info = new TableInfo {Name = "magic"};
                PreserveDuringLinkMagic = info.Name != "magic";
            }
        }

        /// <summary>
        ///   Constructs a new SqliteSessionBase and opens a SQLite database 
        ///   specified by <paramref name="connectionString"/>.
        /// </summary>
        /// <param name = "connectionString">
        ///   Specifies the path to the database file.
        /// </param>
        /// <param name="autoOpen">
        ///   True if the connection must be opened now.
        /// </param>
        public SqliteSessionBase(string connectionString, bool autoOpen = true)
        {
            SessionGuid = Guid.NewGuid();

            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to write all command 
        /// information to the debug log.
        /// </summary>
        public static bool Trace { get; set; }

        /// <summary>
        /// Gets the database connection string for this connection.
        /// </summary>
        public string ConnectionString { get; private set; }

        public abstract void Open();

        #region IDisposable Members

        public virtual void Dispose()
        {
            if (_tables != null)
            {
                foreach (var tbl in _tables)
                {
                    tbl.Value.Dispose();
                }
            }
        }

        #endregion

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
            string typeFullName = type.FullName;

            if (_tables == null)
            {
                _tables = new Dictionary<string, TableMapping>();
            }
            TableMapping map;
            if (!_tables.TryGetValue(typeFullName, out map))
            {
                map = CreateTableMapping(type);
                _tables[typeFullName] = map;
            }
            return map;
        }

        protected abstract TableMapping CreateTableMapping(Type type);

        /// <summary>
        /// Checks to see if a particular table exists.
        /// </summary>
        /// <typeparam name="T">The table to check for.</typeparam>
        /// <returns>
        /// True if the table exists in the database, otherwise False.
        /// </returns>
        public bool TableExists<T>()
        {
            return TableExists(GetMapping<T>().TableName);
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
            return TableExists(GetMapping(type).TableName);
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
        /// <param name="createIndexes"> 
        ///   False if you don't want to automatically create indexes.
        /// </param>
        /// <returns>
        ///   The number of entries added to the database schema.
        /// </returns>
        public int CreateTable<T>(bool createIndexes = true)
        {
            return this.RunInTransaction(() => this.CreateTable(GetMapping<T>(), createIndexes), true);
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
        public int CreateTable(Type type, bool createIndexes = true)
        {
            return this.RunInTransaction(() => this.CreateTable(GetMapping(type), createIndexes), true);
        }

        private int CreateTable(TableMapping map, bool createIndexes)
        {
            // todo: allow index clearing/re-creating

            int count = 0;

            bool exists = this.TableExists(map.TableName);

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
            if (createIndexes)
            {
                count += map.Indexes.Sum(index => Execute(index.GetCreateSql(map.TableName)));
            }

            if (Trace)
            {
                Debug.WriteLine("Updates to the database: {0}", count);
            }

            return count;
        }

        private int MigrateTable(TableMapping map)
        {
            var existingCols = this.GetTableColumns(map).ToArray();
            List<TableMapping.Column> toBeAdded =
                map.Columns
                   .Where(p => existingCols.All(
                       e => !e.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                   .ToList();

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
        ///   Returns the columns, from the mapping, that actually exist in the 
        ///   database.
        /// </summary>
        /// <param name="map">The mapping of the table to use.</param>
        /// <returns>
        ///   The columns that exist in both the mapping and the database.
        /// </returns>
        public IEnumerable<TableMapping.Column> GetTableColumns(TableMapping map)
        {
            string query = string.Format(CultureInfo.InvariantCulture, "PRAGMA table_info([{0}]);", map.TableName);
            var existingCols = Query<TableInfo>(query);
            return map.Columns.Where(p => existingCols.Any(e => e.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)));
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
            return this.ExecuteInternal(new Execution {Args = args, Sql = query});
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
            var scalar = this.ExecuteScalarInternal(new Execution { Args = args, Sql = cmdText });
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
        public object ExecuteScalar(string cmdText, params object[] args)
        {
            return this.ExecuteScalar<object>(cmdText, args);
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
        public IEnumerable<T> DeferredQuery<T>(string query, params object[] args)
        {
            return DeferredQuery(typeof(T), query, args).Cast<T>();
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
        ///   Arguments to substitute for the occurrences of '?' in the query.
        /// </param>
        /// <returns>
        ///   An enumerable with one result for each row returned by the query.
        ///   The enumerator will call sqlite3_step on each call to MoveNext, so the database
        ///   connection must remain open for the lifetime of the enumerator.
        /// </returns>
        public IEnumerable DeferredQuery(Type type, string query, params object[] args)
        {
            var ex = new Execution {Map = GetMapping(type), Sql = query, Args = args};

            return from object result in this.ExecuteDeferredQueryInternal(ex)
                   select result;
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
        /// <param name="type">
        ///   The type of object to return
        /// </param>
        /// <param name = "queryText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurences of '?' in the query.
        /// </param>
        /// <returns>
        ///   An enumerable with one result for each row returned by the query.
        /// </returns>
        public IList Query(Type type, string queryText, params object[] args)
        {
            return DeferredQuery(type, queryText, args).Cast<object>().ToList();
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
            return this.GetListInternal(typeof(T), pk, pks).Cast<T>().ToList();
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
        public IList GetList(Type type, object pk, params object[] pks)
        {
            return this.GetListInternal(type, pk, pks).Cast<object>().ToList();
        }

        private IEnumerable GetListInternal(Type type, object pk, object[] pks)
        {
            var map = this.GetMapping(type);
            if (map.PrimaryKey == null)
            {
                throw new ArgumentException("There are no primary keys");
            }

            var sql = map.GetSelectSql();
            var args = new[] {pk}.Concat(pks).ToArray();

            return this.DeferredQuery(type, sql, args);
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
        ///   Attempts to retrieve an object with the given primary key from the table
        ///   associated with the specified type. Use of this method requires that
        ///   the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="type">The type of object to return.</param>
        /// <param name = "primaryKey">The primary key for 'type'.</param>
        /// <param name = "primaryKeys">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The object with the given primary key. Throws a not found exception if the object is not found.</returns>
        public object Get(Type type, object primaryKey, params object[] primaryKeys)
        {
            return GetList(type, primaryKey, primaryKeys)[0];
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
        ///   Attempts to retrieve an object with the given primary key from the table
        ///   associated with the specified type. Use of this method requires that
        ///   the given type have a designated PrimaryKey (using the PrimaryKeyAttribute).
        /// </summary>
        /// <param name="type">The type of object to return.</param>
        /// <param name = "primaryKey">The primary key for 'T'.</param>
        /// <param name = "primaryKeys">Any addition primary keys for multiple primaryKey tables</param>
        /// <returns>The object with the given primary key or null if the object is not found.</returns>
        public object Find(Type type, object primaryKey, params object[] primaryKeys)
        {
            var list = GetList(type, primaryKey, primaryKeys);
            return list.Count > 0 ? list[0] : null;
        }

        /// <summary>
        /// This function does the actual calls on the database.
        /// </summary>
        protected abstract IEnumerable ExecuteDeferredQueryInternal(Execution execution);
        protected abstract object ExecuteScalarInternal(Execution execution);
        protected abstract int ExecuteInternal(Execution execution);
        
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
        ///   Inserts all specified objects.
        /// </summary>
        /// <param name = "objects">
        ///   An <see cref = "IEnumerable" /> of the objects to insert.
        /// </param>
        /// <param name = "createTransaction">
        ///   True if this operation should create (or use a current) and control a transaction.
        /// </param>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public int InsertAll(IEnumerable objects, bool createTransaction = true)
        {
            return RunInTransaction(() => objects.Cast<object>().Sum(r => this.Insert(r)), createTransaction);
        }

        /// <summary>
        ///   Updates all specified objects.
        /// </summary>
        /// <param name = "objects">
        ///   An <see cref = "IEnumerable" /> of the objects to update.
        /// </param>
        /// <param name = "createTransaction">
        ///   True if this operation should create (or use a current) and control a transaction.
        /// </param>
        /// <returns>
        ///   The number of rows updated in the table.
        /// </returns>
        public int UpdateAll(IEnumerable objects, bool createTransaction = true)
        {
            return RunInTransaction(() => objects.Cast<object>().Sum(r => this.Update(r)), createTransaction);
        }

        protected abstract TResult RunInTransaction<TResult>(Func<TResult> action, bool createTransaction);

        /// <summary>
        ///   Inserts a record in the table with the specified defaults as the column values.
        /// </summary>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public int InsertDefaults<T>()
            where T : class
        {
            return this.InsertDefaults<T>(ConflictResolution.Default);
        }

        /// <summary>
        ///   Inserts a record in the table with the specified defaults as the column values.
        /// </summary>
        /// <returns>
        ///   The number of rows added to the table.
        /// </returns>
        public int InsertDefaults<T>(ConflictResolution extra)
        {
            return this.InsertInternal(typeof(T), extra);
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
        public int Insert(object obj)
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
        public int Insert(object obj, ConflictResolution extra)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj", "Cannot insert a null object.");
            }

            TableMapping map = GetMapping(obj.GetType());

            var count = this.InsertInternal(obj, extra);

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

        protected long GetLastInsertRowId()
        {
            return this.ExecuteScalar<long>("SELECT last_insert_rowid() as Id;");
        }

        protected abstract int InsertInternal(object obj, ConflictResolution extra);

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
        public int Update(object obj)
        {
            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            return this.UpdateInternal(obj);
        }

        protected abstract int UpdateInternal(object obj);

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
        public int Delete(object obj)
        {
            var tracked = obj as ITrackConnection;
            if (tracked != null)
            {
                tracked.Connection = this;
            }

            var args = new List<object>();
            string sql = GetMapping(obj.GetType()).GetDeleteSql(obj, args);
            return Execute(sql, args.ToArray());
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

        #region Nested type: Execution

        protected class Execution
        {
            public string Sql { get; set; }
            public object[] Args { get; set; }
            public TableMapping Map { get; set; }
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

        public abstract object GetExpressionMemberValue(Expression expression, MemberExpression member, object obj);
    }
}