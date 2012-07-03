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
using Mono.Data.Sqlite.Orm.DataConverter;

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

        public TableMapping(Type type)
        {
            MappedType = type;

            TableName = OrmHelper.GetTableName(MappedType);
            OldTableName = OrmHelper.GetOldTableName(MappedType);
            OnPrimaryKeyConflict = OrmHelper.GetOnPrimaryKeyConflict(MappedType);
            _properties = OrmHelper.GetProperties(this.MappedType);
            Columns = _properties.Select(x => new Column(x)).ToList();
            Checks = OrmHelper.GetChecks(this.MappedType);
            ForeignKeys = OrmHelper.GetForeignKeys(_properties);
            Indexes = OrmHelper.GetIndexes(MappedType, _properties);

            Virtual = OrmHelper.GetVirtual(MappedType);
            Tokenizer = OrmHelper.GetTokenizer(MappedType);
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

                Column autoIncCol;
                this.PrimaryKey = OrmHelper.GetPrimaryKey(this.Columns, out autoIncCol);
                this.AutoIncrementColumn = autoIncCol;

                EditableColumns = Columns.Where(c => c != AutoIncrementColumn).ToList();
            }
        }

        public IList<Column> EditableColumns { get; private set; }
        public PrimaryKeyDefinition PrimaryKey { get; private set; }
        public Column AutoIncrementColumn { get; private set; }
        public IList<string> Checks { get; private set; }
        public IList<ForeignKey> ForeignKeys { get; private set; }
        public IList<Index> Indexes { get; private set; }

        public VirtualAttribute Virtual { get; private set; }
        public TokenizerAttribute Tokenizer { get; private set; }

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
                _insertCommand.CommandText = this.GetInsertSql(extra, withDefaults);
                _insertCommand.Prepare();
            }

            return _insertCommand;
        }

        public string GetUpdateSql<T>(T obj, List<object> args)
        {
            return GetUpdateSql(obj, ConflictResolution.Default, args);
        }

        public string GetUpdateSql<T>(T obj, ConflictResolution extra, List<object> args)
        {
            if (PrimaryKey == null)
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
                string pks = string.Join(" AND ", PrimaryKey.Columns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
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
                args.AddRange(PrimaryKey.Columns.Select(c => c.GetValue(obj)));
            }

            return _updateSql;
        }

        public string GetDeleteSql<T>(T obj, List<object> args)
        {
            if (PrimaryKey == null)
            {
                throw new NotSupportedException("Cannot delete from " + TableName + ": it has no primary keys");
            }

            if (string.IsNullOrEmpty(_deleteSql))
            {
                string pks = string.Join(" AND ", PrimaryKey.Columns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                _deleteSql = string.Format(CultureInfo.InvariantCulture, "DELETE FROM [{0}] WHERE {1}", TableName, pks);
            }

            if (args != null)
            {
                args.AddRange(PrimaryKey.Columns.Select(c => c.GetValue(obj)));
            }

            return _deleteSql;
        }

        public string GetUpdateSql(string propertyName, object propertyValue,
                                     List<object> args,
                                     object pk, params object[] pks)
        {
            if (PrimaryKey == null)
            {
                throw new NotSupportedException("Cannot update " + TableName + ": it has no primary keys");
            }

            args.AddRange(new[] {propertyValue, pk}.Concat(pks));

            string whereClause = string.Join(" AND ", PrimaryKey.Columns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
            return string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = ? WHERE {2}", TableName, propertyName, whereClause);
        }

        public string GetUpdateAllSql(string propertyName, object propertyValue, List<object> args)
        {
            args.Add(propertyValue);
            return string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = ?", TableName, propertyName);
        }

        #region Nested type: Column

        public class Column
        {
            private readonly PropertyInfo _prop;

            public PropertyInfo Property
            {
                get { return this._prop; }
            }

            private IDataConverter _dataConverter;

            internal Column(PropertyInfo prop)
            {
                _prop = prop;
                this._dataConverter = null;

                Name = OrmHelper.GetColumnName(prop);
                // If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T,
                // otherwise it returns null, so get the the actual type instead
                ColumnType = OrmHelper.GetColumnType(prop);
                Collation = OrmHelper.GetCollation(prop);
                PrimaryKey = OrmHelper.GetPrimaryKey(prop);
                IsNullable = this.PrimaryKey == null && OrmHelper.GetIsColumnNullable(prop);
                IsAutoIncrement = OrmHelper.GetIsAutoIncrement(prop);
                Unique = OrmHelper.GetUnique(prop);
                MaxStringLength = OrmHelper.GetMaxStringLength(prop);
                Checks = OrmHelper.GetChecks(prop);
                DefaultValue = OrmHelper.GetDefaultValue(prop);
                this.DataConverterAttribute = OrmHelper.GetDataConverter(prop);
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

            public DataConverterAttribute DataConverterAttribute { get; private set; }
            public IDataConverter DataConverter
            {
                get
                {
                    if (this._dataConverter == null && this.DataConverterAttribute != null)
                    {
                        var dc = Activator.CreateInstance(this.DataConverterAttribute.DataConverter);
                        this._dataConverter = (IDataConverter) dc;
                    }

                    return this._dataConverter;
                }
            }

            internal void SetValue(object obj, object val)
            {
                object value = val == DBNull.Value ? null : val;

                if (this.DataConverter != null)
                {
                    value = DataConverter.ConvertBack(value, _prop.PropertyType, this.DataConverterAttribute.Parameter);
                }

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
                    if (this.DataConverter != null)
                    {
                        _prop.SetValue(obj, value, null);
                        return;
                    }

                    object v;

                    if (ColumnType == typeof (Guid))
                    {
                        string text = value.ToString();
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
                object value = _prop.GetValue(obj, null);

                if (this.DataConverter != null)
                {
                    value = DataConverter.Convert(value, _prop.PropertyType, this.DataConverterAttribute.Parameter);
                }
                else if (value != null)
                {
                    value = Convert.ChangeType(value, this.ColumnType, CultureInfo.InvariantCulture);
                }

                return value;
            }

            internal string GetCreateSql(TableMapping table)
            {
                var constraints = new List<string>();

                if (PrimaryKey != null && table.PrimaryKey.Columns.Length == 1)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "{0} PRIMARY KEY {1}",
                                                  string.IsNullOrEmpty(PrimaryKey.Name)
                                                      ? string.Empty
                                                      : string.Format(CultureInfo.InvariantCulture, "CONSTRAINT {0}", PrimaryKey.Name),
                                                  PrimaryKey.Direction));
                    if (table.OnPrimaryKeyConflict != ConflictResolution.Default)
                    {
                        constraints.Add(string.Format(CultureInfo.InvariantCulture, "ON CONFLICT {0}", table.OnPrimaryKeyConflict));
                    }
                    if (table.AutoIncrementColumn == this)
                    {
                        constraints.Add("AUTOINCREMENT");
                    }
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

        #region Nested type: PrimaryKeyDefinition

        public class PrimaryKeyDefinition
        {
            internal PrimaryKeyDefinition()
            {
                Columns = new Column[0];
            }

            public string Name { get; internal set; }
            public Column[] Columns { get; internal set; }
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

    public static class SqliteWriter
    {
        public static string GetInsertSql(this TableMapping table, ConflictResolution extra, bool withDefaults)
        {
            var sb = new StringBuilder();

            sb.Append("INSERT");
            if (extra != ConflictResolution.Default)
            {
                sb.Append(" OR ");
                sb.Append(extra);
            }
            sb.Append(" INTO ");
            sb.Append(Quote(table.TableName));

            if (withDefaults)
            {
                sb.Append(" DEFAULT VALUES");
            }
            else
            {
                sb.Append(" (");
                sb.Append(string.Join(", ", table.EditableColumns.Select(c => Quote(c.Name))));
                sb.Append(") VALUES (");
                sb.Append(string.Join(", ", Enumerable.Repeat("?", table.EditableColumns.Count)));
                sb.Append(")");
            }

            return sb.ToString();
        }

        public static string GetRenameSql(this TableMapping table)
        {
            var sb = new StringBuilder();
            sb.Append("ALTER TABLE ");
            sb.Append(Quote(table.OldTableName));
            sb.Append(" RENAME TO ");
            sb.Append(Quote(table.TableName));
            return sb.ToString();
        }

        public static string GetCreateSql(this TableMapping.Index index, string tableName)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE ");
            if (index.Unique)
            {
                sb.Append("UNIQUE ");
            }
            sb.Append("INDEX ");
            sb.Append(Quote(index.IndexName));
            sb.Append(" on ");
            sb.Append(Quote(tableName));
            sb.AppendLine(" (");
            bool first = true;
            foreach (var column in index.Columns.OrderBy(c => c.Order))
            {
                if (!first)
                {
                    sb.AppendLine(",");
                }
                sb.Append(Quote(column.ColumnName));
                if (column.Collation != Collation.Default)
                {
                    sb.Append(" COLLATE ");
                    sb.Append(column.Collation);
                }
                if (column.Direction != Direction.Default)
                {
                    sb.Append(" ");
                    sb.Append(column.Direction);
                }
                first = false;
            }
            sb.AppendLine();
            sb.Append(");");
            return sb.ToString();
        }

        public static string GetCreateSql(this TableMapping table)
        {
            var sb = new StringBuilder();
            sb.Append("CREATE ");
            if (table.Virtual != null)
            {
                sb.Append("VIRTUAL ");
            }
            sb.Append("TABLE ");
            sb.Append(Quote(table.TableName));
            if (table.Virtual != null)
            {
                sb.Append(" USING ");
                sb.Append(table.Virtual.ModuleName);
            }
            sb.Append(" (");
            sb.AppendLine();
            bool first = true;
            foreach (var column in table.Columns)
            {
                if (!first)
                {
                    sb.AppendLine(",");
                }
                sb.Append(column.GetCreateSql(table));
                first = false;
            }

            if (table.Virtual != null && table.Tokenizer != null)
            {
                sb.AppendLine(",");
                sb.Append("tokenize=");
                sb.Append(table.Tokenizer.FullValue);
            }

            if (table.PrimaryKey != null && table.PrimaryKey.Columns.Length > 1)
            {
                sb.AppendLine(",");
                sb.Append(table.PrimaryKey.GetCreateSql());
            }
            if (table.ForeignKeys.Any())
            {
                sb.AppendLine(",");
                first = true;
                foreach (var key in table.ForeignKeys)
                {
                    if (!first)
                    {
                        sb.AppendLine(",");
                    }
                    sb.Append(key.GetCreateSql());
                    first = false;
                }
            }
            if (table.Checks.Any())
            {
                sb.AppendLine(",");
                first = true;
                foreach (var check in table.Checks)
                {
                    if (!first)
                    {
                        sb.AppendLine(",");
                    }
                    sb.Append("CHECK (");
                    sb.Append(check);
                    sb.Append(")");
                    first = false;
                }
            }
            sb.AppendLine();
            sb.Append(");");
            return sb.ToString();
        }

        public static string GetCreateSql(this TableMapping.PrimaryKeyDefinition primaryKey)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(primaryKey.Name))
            {
                sb.Append("CONSTRAINT ");
                sb.Append(primaryKey.Name);
                sb.AppendLine();
            }
            sb.Append("PRIMARY KEY (");
            bool first = true;
            foreach (var column in primaryKey.Columns)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                sb.Append(Quote(column.Name));
                if (column.PrimaryKey.Direction != Direction.Default)
                {
                    sb.Append(" ");
                    sb.Append(column.PrimaryKey.Direction);
                }
                first = false;
            }
            sb.Append(")");
            return sb.ToString();
        }

        public static string GetCreateSql(this TableMapping.ForeignKey foreignKey)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(foreignKey.Name))
            {
                sb.Append("CONSTRAINT ");
                sb.Append(Quote(foreignKey.Name));
                sb.AppendLine();
            }
            sb.Append("FOREIGN KEY (");
            sb.Append(string.Join(", ", foreignKey.Keys.Keys.Select(Quote)));
            sb.Append(")");
            sb.AppendLine();
            sb.Append("REFERENCES ");
            sb.Append(Quote(foreignKey.ChildTable));
            sb.Append(" (");
            sb.Append(string.Join(", ", foreignKey.Keys.Values.Select(Quote)));
            sb.Append(")");
            if (foreignKey.OnUpdate != ForeignKeyAction.Default)
            {
                sb.AppendLine();
                sb.Append("ON UPDATE ");
                sb.Append(OrmHelper.GetForeignKeyActionString(foreignKey.OnUpdate));
            }
            if (foreignKey.OnDelete != ForeignKeyAction.Default)
            {
                sb.AppendLine();
                sb.Append("ON DELETE ");
                sb.Append(OrmHelper.GetForeignKeyActionString(foreignKey.OnDelete));
            }
            if (foreignKey.NullMatch != NullMatch.Default)
            {
                sb.AppendLine();
                sb.Append("MATCH ");
                sb.Append(foreignKey.NullMatch);
            }
            if (foreignKey.Deferred != Deferred.Default)
            {
                sb.AppendLine();
                sb.Append("DEFERRABLE INITIALLY ");
                sb.Append(foreignKey.Deferred);
            }
            return sb.ToString();
        }

        private static string Quote(string name)
        {
            return "[" + name + "]";
        }
    }
}