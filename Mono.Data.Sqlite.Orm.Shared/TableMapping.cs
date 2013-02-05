using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Mono.Data.Sqlite.Orm.ComponentModel;
using Mono.Data.Sqlite.Orm.DataConverter;

namespace Mono.Data.Sqlite.Orm
{
    public class TableMapping : IDisposable
    {
        private readonly PropertyInfo[] _properties;
        private IList<Column> _columns;
        protected string _deleteSql;
        protected bool _insertDefaults;
        protected ConflictResolution _insertExtra;
        protected ConflictResolution _updateExtra;
        protected string _updateSql;

        public TableMapping(Type type)
        {
            this.MappedType = type;

            this.TableName = OrmHelper.GetTableName(this.MappedType);
            this.OldTableName = OrmHelper.GetOldTableName(this.MappedType);
            this.OnPrimaryKeyConflict = OrmHelper.GetOnPrimaryKeyConflict(this.MappedType);
            this._properties = OrmHelper.GetProperties(this.MappedType);
            this.Columns = this._properties.Select(x => new Column(x)).ToList();
            this.Checks = OrmHelper.GetChecks(this.MappedType);
            this.ForeignKeys = OrmHelper.GetForeignKeys(this._properties);
            this.Indexes = OrmHelper.GetIndexes(this.MappedType, this._properties);
            
            this.Virtual = OrmHelper.GetVirtual(this.MappedType);
            this.Tokenizer = OrmHelper.GetTokenizer(this.MappedType);
        }

        public Type MappedType { get; private set; }
        public string OldTableName { get; private set; }
        public string TableName { get; private set; }
        public ConflictResolution OnPrimaryKeyConflict { get; private set; }

        public IList<Column> Columns
        {
            get { return this._columns; }
            private set
            {
                this._columns = value;

                Column autoIncCol;
                this.PrimaryKey = OrmHelper.GetPrimaryKey(this.Columns, out autoIncCol);
                this.AutoIncrementColumn = autoIncCol;

                this.EditableColumns = this.Columns.Where(c => c != this.AutoIncrementColumn).ToList();
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

        public Column FindColumn(string name)
        {
            return this.Columns.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public string GetUpdateSql()
        {
            return this.GetUpdateSql(ConflictResolution.Default);
        }

        public string GetUpdateSql(ConflictResolution extra)
        {
            if (this.PrimaryKey == null)
            {
                throw new NotSupportedException("Cannot update " + this.TableName + ": it has no primary keys");
            }

            if (!string.IsNullOrEmpty(this._updateSql) && this._updateExtra != extra)
            {
                this._updateSql = string.Empty;
            }

            if (string.IsNullOrEmpty(this._updateSql))
            {
                this._updateExtra = extra;

                string col = string.Join(", ", this.EditableColumns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                string pks = string.Join(" AND ", this.PrimaryKey.Columns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                this._updateSql = string.Format(CultureInfo.InvariantCulture, "UPDATE {3} [{0}] SET {1} WHERE {2}",
                                           this.TableName,
                                           col,
                                           pks,
                                           extra == ConflictResolution.Default
                                               ? string.Empty
                                               : string.Format(CultureInfo.InvariantCulture, "OR {0}", extra));
            }

            return this._updateSql;
        }

        public string GetDeleteSql<T>(T obj, List<object> args)
        {
            if (this.PrimaryKey == null)
            {
                throw new NotSupportedException("Cannot delete from " + this.TableName + ": it has no primary keys");
            }

            if (string.IsNullOrEmpty(this._deleteSql))
            {
                string pks = string.Join(" AND ", this.PrimaryKey.Columns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
                this._deleteSql = string.Format(CultureInfo.InvariantCulture, "DELETE FROM [{0}] WHERE {1}", this.TableName, pks);
            }

            if (args != null)
            {
                args.AddRange(this.PrimaryKey.Columns.Select(c => c.GetValue(obj)));
            }

            return this._deleteSql;
        }

        public string GetUpdateSql(string propertyName, object propertyValue,
                                   List<object> args,
                                   object pk, params object[] pks)
        {
            if (this.PrimaryKey == null)
            {
                throw new NotSupportedException("Cannot update " + this.TableName + ": it has no primary keys");
            }

            args.AddRange(new[] {propertyValue, pk}.Concat(pks));

            string whereClause = string.Join(" AND ", this.PrimaryKey.Columns.Select(c => string.Format(CultureInfo.InvariantCulture, "[{0}] = ?", c.Name)).ToArray());
            return string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = ? WHERE {2}", this.TableName, propertyName, whereClause);
        }

        public string GetUpdateAllSql(string propertyName, object propertyValue, List<object> args)
        {
            args.Add(propertyValue);
            return string.Format(CultureInfo.InvariantCulture, "UPDATE [{0}] SET [{1}] = ?", this.TableName, propertyName);
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
                this._prop = prop;
                this._dataConverter = null;

                this.Name = OrmHelper.GetColumnName(prop);
                // If this type is Nullable<T> then Nullable.GetUnderlyingType returns the T,
                // otherwise it returns null, so get the the actual type instead
                this.ColumnType = OrmHelper.GetColumnType(prop);
                this.EnumType = OrmHelper.GetEnumType(prop);
                this.Collation = OrmHelper.GetCollation(prop);
                this.PrimaryKey = OrmHelper.GetPrimaryKey(prop);
                this.IsNullable = this.PrimaryKey == null && OrmHelper.GetIsColumnNullable(prop);
                this.IsAutoIncrement = OrmHelper.GetIsAutoIncrement(prop);
                this.Unique = OrmHelper.GetUnique(prop);
                this.MaxStringLength = OrmHelper.GetMaxStringLength(prop);
                this.Checks = OrmHelper.GetChecks(prop);
                this.DefaultValue = OrmHelper.GetDefaultValue(prop);
                this.DataConverterAttribute = OrmHelper.GetDataConverter(prop);
            }

            public string Name { get; private set; }
            public Type ColumnType { get; private set; }
            public Collation Collation { get; private set; }
            public PrimaryKeyAttribute PrimaryKey { get; private set; }
            public bool IsAutoIncrement { get; private set; }
            public bool IsNullable { get; private set; }
            public Type EnumType { get; private set; }
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

            public void SetValue(object obj, object value)
            {
                // TODO: this may need to change
                if (value.GetType().FullName == "System.DBNull")
                {
                    value = null;
                }

                if (this.DataConverter != null)
                {
                    value = this.DataConverter.ConvertBack(value, this._prop.PropertyType, this.DataConverterAttribute.Parameter);
                }

                if (value == null)
                {
                    if (this.IsNullable)
                    {
                        this._prop.SetValue(obj, null, null);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                                                                          "Unable to assign a NULL value to non nullable column: {0}.{1} ({2})",
                                                                          this.GetType().Name, this.Name, this.ColumnType));
                    }
                }
                else
                {
                    if (this.DataConverter != null)
                    {
                        this._prop.SetValue(obj, value, null);
                        return;
                    }

                    object v;

                    if (this._prop.PropertyType == typeof(Guid))
                    {
                        string text = value.ToString();
                        v = text.Length > 0 ? new Guid(text) : Guid.Empty;
                    }
                    else if (this.EnumType != null)
                    {
                        v = Enum.Parse(this.EnumType, value.ToString(), true);
                    }
                    else if (this._prop.PropertyType == typeof(TimeSpan))
                    {
                        v = TimeSpan.FromTicks((long)value);
                    }
                    else
                    {
                        v = Convert.ChangeType(value, this.ColumnType, CultureInfo.CurrentCulture);
                    }

                    this._prop.SetValue(obj, v, null);
                }
            }

            public object GetValue(object obj)
            {
                object value = this._prop.GetValue(obj, null);

                if (this.DataConverter != null)
                {
                    value = this.DataConverter.Convert(value, this._prop.PropertyType, this.DataConverterAttribute.Parameter);
                }

                if (this._prop.PropertyType == typeof(TimeSpan))
                {
                    value = ((TimeSpan)value).Ticks;
                }
                else if (this._prop.PropertyType == typeof(Guid))
                {
                    value = value.ToString();
                }
                
                if (value != null)
                {
                    value = Convert.ChangeType(value, this.ColumnType, CultureInfo.InvariantCulture);
                }

                return value;
            }

            public string GetCreateSql(TableMapping table)
            {
                var constraints = new List<string>();

                if (this.PrimaryKey != null && table.PrimaryKey.Columns.Length == 1)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "{0} PRIMARY KEY {1}",
                                                  string.IsNullOrEmpty(this.PrimaryKey.Name)
                                                      ? string.Empty
                                                      : string.Format(CultureInfo.InvariantCulture, "CONSTRAINT {0}", this.PrimaryKey.Name),
                                                  this.PrimaryKey.Direction));
                    if (table.OnPrimaryKeyConflict != ConflictResolution.Default)
                    {
                        constraints.Add(string.Format(CultureInfo.InvariantCulture, "ON CONFLICT {0}", table.OnPrimaryKeyConflict));
                    }
                    if (table.AutoIncrementColumn == this)
                    {
                        constraints.Add("AUTOINCREMENT");
                    }
                }

                if (this.Unique != null)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "UNIQUE {0}",
                                                  this.Unique.OnConflict != ConflictResolution.Default
                                                      ? string.Format(CultureInfo.InvariantCulture, "ON CONFLICT {0}", this.Unique.OnConflict)
                                                      : string.Empty));
                }
                if (!this.IsNullable)
                {
                    constraints.Add("NOT NULL");
                }
                if (this.Checks.Any())
                {
                    constraints.Add(string.Join(" ", this.Checks.Select(c => string.Format(CultureInfo.InvariantCulture, "CHECK ({0})", c)).ToArray()));
                }
                if (!string.IsNullOrEmpty(this.DefaultValue))
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "DEFAULT({0})", this.DefaultValue));
                }
                if (this.Collation != Collation.Default)
                {
                    constraints.Add(string.Format(CultureInfo.InvariantCulture, "COLLATE {0}", this.Collation));
                }

                return string.Format(CultureInfo.InvariantCulture, "[{0}] {1} {2}", this.Name, OrmHelper.SqlType(this), string.Join(" ", constraints.ToArray()));
            }
        }

        #endregion

        #region Nested type: PrimaryKeyDefinition

        public class PrimaryKeyDefinition
        {
            internal PrimaryKeyDefinition()
            {
                this.Columns = new Column[0];
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
                this.Keys = new Dictionary<string, string>();
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

                this.IndexName = name;
                this.Columns = new List<IndexedColumn>();
            }

            public string IndexName { get; private set; }
            public bool Unique { get; internal set; }
            public IList<IndexedColumn> Columns { get; private set; }

            #region Nested type: IndexedColumn

            public class IndexedColumn
            {
                internal IndexedColumn(string columnName)
                {
                    this.ColumnName = columnName;
                }

                public string ColumnName { get; private set; }
                public int Order { get; internal set; }
                public Collation Collation { get; internal set; }
                public Direction Direction { get; internal set; }
            }

            #endregion
        }

        #endregion

        public void Dispose()
        {
            
        }
    }
}