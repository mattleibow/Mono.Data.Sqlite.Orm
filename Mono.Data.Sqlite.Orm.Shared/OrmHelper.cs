using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public static class OrmHelper
    {
        internal const int DefaultMaxStringLength = -1;

        internal static string GetForeignKeyActionString(ForeignKeyAction action)
        {
            string result = string.Empty;

            switch (action)
            {
                case ForeignKeyAction.Cascade:
                    result = "CASCADE";
                    break;
                case ForeignKeyAction.NoAction:
                    result = "NO ACTION";
                    break;
                case ForeignKeyAction.Restrict:
                    result = "RESTRICT";
                    break;
                case ForeignKeyAction.SetDefault:
                    result = "SET DEFAULT";
                    break;
                case ForeignKeyAction.SetNull:
                    result = "SET NULL";
                    break;
            }

            return result;
        }

        internal static string SqlType(TableMapping.Column p)
        {
            Type clrType = p.ColumnType;
            int len = p.MaxStringLength;
            if (clrType == typeof(Char))
            {
                clrType = typeof (String);
                len = 1;
            }

            if (clrType == typeof (Boolean) ||
                clrType == typeof (Byte) ||
                clrType == typeof (UInt16) ||
                clrType == typeof (SByte) ||
                clrType == typeof (Int16) ||
                clrType == typeof (Int32) ||
                clrType.IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof (UInt32) ||
                clrType == typeof (TimeSpan) ||
                clrType == typeof (Int64))
            {
                return "bigint";
            }
            if (clrType == typeof (Single) ||
                clrType == typeof (Double) ||
                clrType == typeof (Decimal))
            {
                return "float";
            }
            if (clrType == typeof(String))
            {
                return (len <= 0)
                           ? "text"
                           : "varchar(" + len + ")";
            }
            if (clrType == typeof (DateTime))
            {
                return "datetime";
            }
            if (clrType == typeof (Byte[]))
            {
                return "blob";
            }
            if (clrType == typeof (Guid))
            {
                return "varchar(30)";
            }

            throw new NotSupportedException("Don't know about " + clrType);
        }

        private static T[] GetAttributes<T>(this MemberInfo type)
        {
            return type.GetCustomAttributes(typeof (T), false).Cast<T>().ToArray();
        }

        private static IEnumerable<PropertyInfo> GetMappableProperties(this Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            return type.GetProperties(flags);
        }

        internal static string GetTableName(Type info)
        {
            string tableName = info.Name;

            TableAttribute[] attrs = info.GetAttributes<TableAttribute>();
            if (attrs.Any())
            {
                string attributeTableName = attrs.First().Name;
                if (!string.IsNullOrEmpty(attributeTableName))
                {
                    tableName = attributeTableName;
                }
            }

            return tableName;
        }

        internal static string GetOldTableName(Type info)
        {
            RenameTableAttribute[] attrs = info.GetAttributes<RenameTableAttribute>();
            return attrs.Any() ? attrs.First().OldName : string.Empty;
        }

        internal static ConflictResolution GetOnPrimaryKeyConflict(Type info)
        {
            TableAttribute[] attrs = info.GetAttributes<TableAttribute>();
            return attrs.Any() ? attrs.First().OnPrimaryKeyConflict : ConflictResolution.Default;
        }

        internal static IList<TableMapping.Index> GetIndexes(Type info, PropertyInfo[] properties)
        {
            var indices = new List<TableMapping.Index>();

            IndexAttribute tblAtt = info.GetAttributes<IndexAttribute>().FirstOrDefault();
            if (tblAtt != null)
            {
                if (indices.Any(i => i.IndexName == tblAtt.Name))
                {
                    throw new NotSupportedException("Only one index attribute per index allowed on a table class.");
                }

                indices.Add(new TableMapping.Index(tblAtt.Name) {Unique = tblAtt.Unique});
            }

            foreach (PropertyInfo prop in properties)
            {
                foreach (IndexedAttribute att in prop.GetAttributes<IndexedAttribute>())
                {
                    TableMapping.Index index = indices.FirstOrDefault(x => x.IndexName == att.Name);
                    if (index == null)
                    {
                        index = new TableMapping.Index(att.Name);
                        indices.Add(index);
                    }

                    var indexedCol = new TableMapping.Index.IndexedColumn(GetColumnName(prop))
                                         {
                                             Order = att.Order,
                                             Collation = att.Collation,
                                             Direction = att.Direction
                                         };
                    index.Columns.Add(indexedCol);
                }
            }

            return indices;
        }

        internal static PropertyInfo[] GetProperties(Type mappedType)
        {
            return (from p in mappedType.GetMappableProperties()
                    let ignore = p.GetAttributes<IgnoreAttribute>().Any()
                    where p.CanWrite && !ignore
                    select p).ToArray();
        }

        internal static TableMapping.PrimaryKeyDefinition GetPrimaryKey(
            IEnumerable<TableMapping.Column> columns, 
            out TableMapping.Column autoIncCol)
        {
            TableMapping.PrimaryKeyDefinition primaryKey = null;
            autoIncCol = null;

            var pkCols = columns.Where(c => c.PrimaryKey != null).OrderBy(c => c.PrimaryKey.Order).ToArray();
            if (pkCols.Any())
            {
                primaryKey = new TableMapping.PrimaryKeyDefinition
                    {
                        Columns = pkCols,
                        Name = GetPrimaryKeyName(pkCols)
                    };
                var autoInc = primaryKey.Columns.Where(c => c.IsAutoIncrement).ToArray();
                if (autoInc.Count() > 1)
                {
                    throw new ArgumentException("Only one property can be an auto incrementing primary key");
                }
                autoIncCol = autoInc.FirstOrDefault();
            }

            return primaryKey;
        }

        public static bool IsNullOrWhitespace(this string text)
        {
            return string.IsNullOrEmpty(text) || string.IsNullOrEmpty(text.Trim());
        }

        private static string GetPrimaryKeyName(IEnumerable<TableMapping.Column> pkCols)
        {
            var pkNameCol = pkCols.FirstOrDefault(c => !c.PrimaryKey.Name.IsNullOrWhitespace());
            string pkName = pkNameCol == null ? null : pkNameCol.PrimaryKey.Name;
            return pkName;
        }

        internal static bool GetIsColumnNullable(PropertyInfo prop)
        {
            Type propertyType = prop.PropertyType;
            Type nullableType = Nullable.GetUnderlyingType(propertyType);

            return (nullableType != null || !propertyType.IsValueType) &&
                   !prop.GetAttributes<NotNullAttribute>().Any();
        }

        internal static List<string> GetChecks(Type mappedType)
        {
            return mappedType.GetAttributes<CheckAttribute>().Select(x => x.Expression).ToList();
        }

        internal static IList<TableMapping.ForeignKey> GetForeignKeys(PropertyInfo[] properties)
        {
            var foreignKeys = new List<TableMapping.ForeignKey>();

            var attributes = from attribute in
                                 (from p in properties
                                  from att in p.GetAttributes<ForeignKeyAttribute>()
                                  select new
                                             {
                                                 Attribute = att,
                                                 Property = p,
                                                 att.Order
                                             })
                             orderby attribute.Order
                             select attribute;

            foreach (var att in attributes)
            {
                TableMapping.ForeignKey key = foreignKeys.FirstOrDefault(fk => fk.Name == att.Attribute.Name);
                var childTable = att.Attribute.ChildTable;
                if (key == null)
                {
                    key = new TableMapping.ForeignKey
                              {
                                  Name = att.Attribute.Name,
                                  ChildTable = GetTableName(childTable),
                                  OnDelete = att.Attribute.OnDeleteAction,
                                  OnUpdate = att.Attribute.OnUpdateAction,
                                  NullMatch = att.Attribute.NullMatch,
                                  Deferred = att.Attribute.Deferred
                              };
                    foreignKeys.Add(key);
                }

                var childProps = childTable.GetMappableProperties();
                var childProp = childProps.SingleOrDefault(x => x.Name == att.Attribute.ChildKey);

                if (childProp == null)
                {
                    throw new ArgumentException(
                        string.Format("Property {0} does not exist in type {1}.",
                                      att.Attribute.ChildKey, childTable.FullName));
                }

                key.Keys.Add(GetColumnName(att.Property), GetColumnName(childProp));
            }

            return foreignKeys;
        }

        internal static DataConverterAttribute GetDataConverter(PropertyInfo prop)
        {
            return prop.GetAttributes<DataConverterAttribute>().FirstOrDefault();
        }

        internal static string[] GetChecks(PropertyInfo prop)
        {
            return prop.GetAttributes<CheckAttribute>().Select(x => x.Expression).ToArray();
        }

        internal static UniqueAttribute GetUnique(PropertyInfo prop)
        {
            return prop.GetAttributes<UniqueAttribute>().FirstOrDefault();
        }

        internal static bool GetIsAutoIncrement(PropertyInfo prop)
        {
            return prop.GetAttributes<AutoIncrementAttribute>().Any();
        }

        internal static PrimaryKeyAttribute GetPrimaryKey(PropertyInfo prop)
        {
            return prop.GetAttributes<PrimaryKeyAttribute>().FirstOrDefault();
        }

        internal static string GetDefaultValue(MemberInfo info)
        {
            DefaultAttribute[] attrs = info.GetAttributes<DefaultAttribute>().ToArray();
            return attrs.Any() ? attrs.First().Value : null;
        }

        internal static Collation GetCollation(MemberInfo info)
        {
            CollationAttribute[] attrs = info.GetAttributes<CollationAttribute>().ToArray();
            return attrs.Any() ? attrs.First().Collation : Collation.Default;
        }

        public static string GetColumnName(MemberInfo info)
        {
            string name = info.Name;

            ColumnAttribute[] attrs = info.GetAttributes<ColumnAttribute>().ToArray();
            if (attrs.Any())
            {
                string attributeName = attrs.First().Name;
                if (!string.IsNullOrEmpty(attributeName))
                {
                    name = attributeName;
                }
            }

            return name;
        }

        internal static int GetMaxStringLength(MemberInfo info)
        {
            MaxLengthAttribute[] attrs = info.GetAttributes<MaxLengthAttribute>().ToArray();
            int maxLength = attrs.Any()
                                ? attrs.First().Length
                                : DefaultMaxStringLength;
            return maxLength;
        }

        internal static Type GetColumnType(PropertyInfo prop)
        {
            var type = GetRealType(prop);
            if (type.IsEnum)
            {
                var attribute = prop.GetAttributes<EnumAffinityAttribute>().FirstOrDefault();
                type = attribute == null ? typeof(int) : attribute.Type;
            }
            else if (type == typeof(TimeSpan))
            {
                type = typeof(long);
            }
            else if (type == typeof(Guid))
            {
                type = typeof(string);
            }

            return type;
        }

        internal static Type GetEnumType(PropertyInfo prop)
        {
            var type = GetRealType(prop);
            if (type.IsEnum)
            {
                return type;
            }
            return null;
        }

        private static Type GetRealType(PropertyInfo prop)
        {
            Type type = null;
            DataConverterAttribute[] attrs = prop.GetAttributes<DataConverterAttribute>().ToArray();
            if (attrs.Any())
            {
                type = attrs.First().StorageType;
            }
            else
            {
                type = prop.PropertyType;
            }
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        public static VirtualAttribute GetVirtual(Type mappedType)
        {
            var attrs = mappedType.GetAttributes<VirtualAttribute>();
            return attrs.FirstOrDefault();
        }

        public static TokenizerAttribute GetTokenizer(Type mappedType)
        {
            var attrs = mappedType.GetAttributes<TokenizerAttribute>();
            return attrs.FirstOrDefault();
        }
    }
}