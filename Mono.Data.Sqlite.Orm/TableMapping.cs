using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Data.Sqlite.Orm.ComponentModel;

#if WINDOWS_PHONE || SILVERLIGHT || NETFX_CORE
using Community.CsharpSqlite.SQLiteClient;
#endif

namespace Mono.Data.Sqlite.Orm
{
    public class TableMapping
    {
        private readonly PropertyInfo[] _properties;
        private IList<Column> _columns;
        private string _deleteSql;
        private DbCommand _insertCommand;
        private ConflictResolution _insertExtra;
        private ConflictResolution _updateExtra;
        private string _updateSql;

        internal TableMapping(Type type)
        {
            MappedType = type;

            TableName = OrmHelper.GetTableName(MappedType);
            OldTableName = OrmHelper.GetOldTableName(MappedType);
            OnPrimaryKeyConflict = OrmHelper.GetOnPrimaryKeyConflict(MappedType);
            _properties = (from p in MappedType.GetMappableProperties()
                           let ignore = p.GetAttributes<IgnoreAttribute>().Any()
                           where p.CanWrite && !ignore
                           select p).ToArray();
            Columns = _properties.Select(x => new Column(x)).ToList();
            Checks = MappedType.GetTypeInfo().GetAttributes<CheckAttribute>().Select(x => x.Expression).ToList();
            ForeignKeys = OrmHelper.GetForeignKeys(_properties);
            Indexes = OrmHelper.GetIndexes(MappedType, _properties);
        }

        public Type MappedType { get; private set; }
        public string OldTableName { get; private set; }
        public string TableName { get; private set; }
        public ConflictResolution OnPrimaryKeyConflict { get; private set; }

        public IList<Column> Columns
        {
            get { return _columns; }
            private set
            {
                _columns = value;

                PrimaryKeys = Columns.Where(c => c.PrimaryKey != null).OrderBy(c => c.PrimaryKey.Order).ToArray();
                Column[] autoInc = PrimaryKeys.Where(c => c.IsAutoIncrement).ToArray();
                if (autoInc.Count() > 1)
                {
                    throw new SqliteException((int)SQLiteErrorCode.Error, "Only one property can be an auto incrementing primary key");
                }
                AutoIncrementColumn = autoInc.FirstOrDefault();

                EditableColumns = Columns.Where(c => c != AutoIncrementColumn).ToList();
            }
        }

        public IList<Column> EditableColumns { get; private set; }
        public IList<Column> PrimaryKeys { get; private set; }
        public Column AutoIncrementColumn { get; private set; }
        public IList<string> Checks { get; private set; }
        public IList<ForeignKey> ForeignKeys { get; private set; }
        public IList<Index> Indexes { get; private set; }

        internal Column FindColumn(string name)
        {
            return Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand GetInsertCommand(DbConnection connection, ConflictResolution extra, bool withDefaults)
        {
            if (_insertCommand != null && _insertExtra != extra)
            {
                if (SqliteSession.Trace)
                {
                    Debug.WriteLine(string.Format("Destroying Insert command for {0} ({1})", TableName, MappedType));
                }

                _insertCommand.Dispose();
                _insertCommand = null;
            }

            if (_insertCommand == null)
            {
                _insertExtra = extra;

                if (SqliteSession.Trace)
                {
                    Debug.WriteLine(string.Format("Creating Insert command for {0} ({1})", TableName, MappedType));
                }

                _insertCommand = connection.CreateCommand();
                _insertCommand.CommandText = GetInsertSql(extra, withDefaults);
                _insertCommand.Prepare();
            }

            return _insertCommand;
        }

        private string GetInsertSql(ConflictResolution extra, bool withDefaults)
        {
            var extraText = extra == ConflictResolution.Default
                                ? string.Empty
                                : string.Format(CultureInfo.InvariantCulture, "OR {0}", extra);

            string commandText;

            if (withDefaults)
            {
                commandText = string.Format(CultureInfo.InvariantCulture, "INSERT {1} INTO [{0}] DEFAULT VALUES",
                                            TableName,
                                            extraText);
            }
            else
            {
                var colNames = EditableColumns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}]", c.Name));
                string columns = string.Join(",", colNames.ToArray());
                commandText = string.Format(CultureInfo.InvariantCulture, "INSERT {3} INTO [{0}] ({1}) VALUES ({2})",
                                            TableName,
                                            columns,
                                            string.Join(",", EditableColumns.Select(c => "?").ToArray()),
                                            extraText);
            }

            return commandText;
        }

        internal string GetUpdateSql<T>(T obj, List<object> args)
        {
            return GetUpdateSql(obj, ConflictResolution.Default, args);
        }

        internal string GetUpdateSql<T>(T obj, ConflictResolution extra, List<object> args)
        {
            if (!PrimaryKeys.Any())
            {
                throw new NotSupportedException("Cannot update " + TableName + ": it has no primary keys");
            }

            if (!string.IsNullOrEmpty(_updateSql) && _updateExtra != extra)
            {
                _updateSql = string.Empty;
            }

            if (string.IsNullOrEmpty(_updateSql))
            {
                _updateExtra = extra;

                string col = string.Join(", ", EditableColumns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                string pks = string.Join(" AND ", PrimaryKeys.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                _updateSql = string.Format(CultureInfo.InvariantCulture, "UPDATE {3} [{0}] SET {1} WHERE {2}",
                                           TableName,
                                           col,
                                           pks,
                                           extra == ConflictResolution.Default
                                               ? string.Empty
                                               : string.Format(CultureInfo.InvariantCulture, "OR {0}", extra));
            }

            if (args != null)
            {
                args.AddRange(EditableColumns.Select(c => c.GetValue(obj)));
                args.AddRange(PrimaryKeys.Select(c => c.GetValue(obj)));
            }

            return _updateSql;
        }

        internal string GetDeleteSql<T>(T obj, List<object> args)
        {
            if (!PrimaryKeys.Any())
            {
                throw new NotSupportedException("Cannot delete from " + TableName + ": it has no primary keys");
            }

            if (string.IsNullOrEmpty(_deleteSql))
            {
                string pks = string.Join(" AND ", PrimaryKeys.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                _deleteSql = string.Format(CultureInfo.InvariantCulture, "DELETE FROM [{0}] WHERE {1}", TableName, pks);
            }

            if (args != null)
            {
                args.AddRange(PrimaryKeys.Select(c => c.GetValue(obj)));
            }

            return _deleteSql;
        }

        internal string GetUpdateSql(string propertyName, object propertyValue,
                                     List<object> args,
                                     object pk, params object[] pks)
        {
            if (!PrimaryKeys.Any())
            {
                throw new NotSupportedException("Cannot update " + TableName + ": it has no primary keys");
            }

            args.AddRange(new[] {propertyValue, pk}.Concat(pks));

            string whereClause = string.Join(" AND ", PrimaryKeys.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
            return string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = ? WHERE {2}", TableName, propertyName, whereClause);
        }

        internal string GetUpdateAllSql(string propertyName, object propertyValue, List<object> args)
        {
            args.Add(propertyValue);
            return string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = ?", TableName, propertyName);
        }

        internal string GetRenameSql()
        {
            return string.Format(CultureInfo.InvariantCulture, "ALTER TABLE [{0}] RENAME TO [{1}]", OldTableName, TableName);
        }

        internal string GetCreateSql()
        {
            var constraints = new List<string>
                                  {
                                      string.Join(",\n", Columns.Select(c => c.GetCreateSql(this)).ToArray())
                                  };
            if (PrimaryKeys.Count() > 1)
            {
                constraints.Add(string.Format(CultureInfo.InvariantCulture, "PRIMARY KEY ({0}) {1}",
                                              string.Join(", ", PrimaryKeys.Select(pk => pk.Name).ToArray()),
                                              OnPrimaryKeyConflict == ConflictResolution.Default
                                                  ? string.Empty
                                                  : string.Format(CultureInfo.InvariantCulture, "ON CONFLICT {0}", OnPrimaryKeyConflict)));
            }
            if (Checks.Any())
            {
                constraints.Add(string.Join(" ", Checks.Select(c => string.Format(CultureInfo.InvariantCulture, "CHECK ({0})", c)).ToArray()));
            }
            if (ForeignKeys.Any())
            {
                constraints.Add(string.Join(" ", ForeignKeys.Select(fk => fk.GetCreateSql()).ToArray()));
            }
            string definition = string.Join(",\n", constraints.ToArray()); // cols, pk, fk, chk

            return string.Format(CultureInfo.InvariantCulture, "CREATE TABLE [{0}] (\n{1});", TableName, definition);
        }

        #region Nested type: Column

        public class Column
        {
            private readonly PropertyInfo _prop;

            internal Column(PropertyInfo prop)
            {
                _prop = prop;

                Type nullableType = Nullable.GetUnderlyingType(prop.PropertyType);

                Name = OrmHelper.GetColumnName(prop);
                // If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T,
                // otherwise it returns null, so get the the actual type instead
                ColumnType = nullableType ?? prop.PropertyType;
                Collation = OrmHelper.GetCollation(prop);
                PrimaryKey = prop.GetAttributes<PrimaryKeyAttribute>().FirstOrDefault();
                IsNullable = PrimaryKey == null &&
                             (nullableType != null || !_prop.PropertyType.GetTypeInfo().IsValueType) &&
                             !prop.GetAttributes<NotNullAttribute>().Any();
                IsAutoIncrement = prop.GetAttributes<AutoIncrementAttribute>().Any();
                Unique = prop.GetAttributes<UniqueAttribute>().FirstOrDefault();
                MaxStringLength = OrmHelper.GetMaxStringLength(prop);
                Checks = prop.GetAttributes<CheckAttribute>().Select(x => x.Expression).ToArray();
                DefaultValue = OrmHelper.GetDefaultValue(prop);
            }

            public string Name { get; private set; }
            public Type ColumnType { get; private set; }
            public Collation Collation { get; private set; }
            public PrimaryKeyAttribute PrimaryKey { get; private set; }
            public bool IsAutoIncrement { get; private set; }
            public bool IsNullable { get; private set; }
            public UniqueAttribute Unique { get; private set; }
            public int MaxStringLength { get; private set; }
            public string DefaultValue { get; private set; }
            public IList<string> Checks { get; private set; }

            internal void SetValue(object obj, object val)
            {
                object value = val == DBNull.Value ? null : val;

                if (value == null)
                {
                    if (IsNullable)
                    {
                        _prop.SetValue(obj, null, null);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                          "Unable to assign a NULL value to non nullable column: {0}.{1} ({2})",
                                                                          GetType().Name, Name, ColumnType));
                    }
                }
                else
                {
                    string text = value.ToString();

                    object v;

                    if (ColumnType == typeof (Guid))
                    {
                        v = text.Length > 0 ? new Guid(text) : Guid.Empty;
                    }
                    else if (ColumnType.GetTypeInfo().IsEnum)
                    {
                        v = Enum.Parse(ColumnType, value.ToString(), true);
                    }
                    else
                    {
                        v = Convert.ChangeType(value, ColumnType, CultureInfo.CurrentCulture);
                    }

                    _prop.SetValue(obj, v, null);
                }
            }

            internal object GetValue(object obj)
            {
                return _prop.GetValue(obj, null);
            }

            internal string GetCreateSql(TableMapping table)
            {
                var constraints = new List<string>();

                if (table.AutoIncrementColumn == this)
                {
                    constraints.Add("PRIMARY KEY AUTOINCREMENT");
                }
                else if (PrimaryKey != null && table.PrimaryKeys.Count <= 1)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "{0} PRIMARY KEY {1}",
                                                  string.IsNullOrEmpty(PrimaryKey.Name)
                                                      ? string.Empty
                                                      : string.Format(CultureInfo.InvariantCulture, "CONSTRAINT {0}", PrimaryKey.Name),
                                                  PrimaryKey.Direction));
                }

                if (Unique != null)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "UNIQUE {0}",
                                                  Unique.OnConflict != ConflictResolution.Default
                                                      ? string.Format(CultureInfo.InvariantCulture, "ON CONFLICT {0}", Unique.OnConflict)
                                                      : string.Empty));
                }
                if (!IsNullable)
                {
                    constraints.Add("NOT NULL");
                }
                if (Checks.Any())
                {
                    constraints.Add(string.Join(" ", Checks.Select(c => string.Format(CultureInfo.InvariantCulture, "CHECK ({0})", c)).ToArray()));
                }
                if (!string.IsNullOrEmpty(DefaultValue))
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "DEFAULT({0})", DefaultValue));
                }
                if (Collation != Collation.Default)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "COLLATE {0}", Collation));
                }

                return string.Format(CultureInfo.InvariantCulture, "[{0}] {1} {2}", Name, OrmHelper.SqlType(this), string.Join(" ", constraints.ToArray()));
            }
        }

        #endregion

        #region Nested type: ForeignKey

        public class ForeignKey
        {
            internal ForeignKey()
            {
                Keys = new Dictionary<string, string>();
            }

            public string Name { get; internal set; }
            public string ChildTable { get; internal set; }
            public Dictionary<string, string> Keys { get; internal set; }
            public ForeignKeyAction OnDelete { get; internal set; }
            public ForeignKeyAction OnUpdate { get; internal set; }
            public NullMatch NullMatch { get; internal set; }
            public Deferred Deferred { get; internal set; }

            internal string GetCreateSql()
            {
                var sb = new StringBuilder();

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0} FOREIGN KEY ({1})",
                                            string.IsNullOrEmpty(Name)
                                                ? string.Empty
                                                : string.Format(CultureInfo.InvariantCulture, "CONSTRAINT {0}", Name),
                                            string.Join(", ", Keys.Keys.ToArray())));
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "REFERENCES {0} ({1})",
                                            ChildTable,
                                            string.Join(", ", Keys.Values.ToArray())));
                if (OnUpdate != ForeignKeyAction.Default)
                {
                    sb.Append("ON UPDATE ");
                    sb.AppendLine(OrmHelper.GetForeignKeyActionString(OnUpdate));
                }
                if (OnDelete != ForeignKeyAction.Default)
                {
                    sb.Append("ON DELETE ");
                    sb.AppendLine(OrmHelper.GetForeignKeyActionString(OnDelete));
                }
                if (NullMatch != NullMatch.Default)
                {
                    sb.Append("MATCH ");
                    sb.AppendLine(NullMatch.ToString());
                }
                if (Deferred != Deferred.Default)
                {
                    sb.Append("DEFERRABLE INITIALLY ");
                    sb.AppendLine(Deferred.ToString());
                }

                return sb.ToString();
            }
        }

        #endregion

        #region Nested type: Index

        public class Index
        {
            internal Index(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new NotSupportedException("All indexes need a name.");
                }

                IndexName = name;
                Columns = new List<IndexedColumn>();
            }

            public string IndexName { get; private set; }
            public bool Unique { get; internal set; }
            public IList<IndexedColumn> Columns { get; private set; }

            internal string GetCreateSql(string tableName)
            {
                IEnumerable<string> cols = from c in Columns
                                           orderby c.Order
                                           select string.Format(CultureInfo.InvariantCulture, "[{0}] {1} {2}",
                                                                c.ColumnName,
                                                                c.Collation == Collation.Default
                                                                    ? string.Empty
                                                                    : string.Format(CultureInfo.InvariantCulture, "COLLATE {0}", c.Collation),
                                                                c.Direction == Direction.Default
                                                                    ? string.Empty
                                                                    : c.Direction.ToString());
                string columnDefs = string.Join(",\n", cols.ToArray());

                return string.Format(CultureInfo.InvariantCulture, "CREATE {0} INDEX [{1}] on [{2}] (\n{3});",
                                     Unique ? "UNIQUE" : string.Empty,
                                     IndexName,
                                     tableName,
                                     columnDefs);
            }

            #region Nested type: IndexedColumn

            public class IndexedColumn
            {
                internal IndexedColumn(string columnName)
                {
                    ColumnName = columnName;
                }

                public string ColumnName { get; private set; }
                public int Order { get; internal set; }
                public Collation Collation { get; internal set; }
                public Direction Direction { get; internal set; }
            }

            #endregion
        }

        #endregion
    }
}