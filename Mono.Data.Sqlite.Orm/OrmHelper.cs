using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public static class OrmHelper
    {
        public const int DefaultMaxStringLength = -1;

        public static string GetForeignKeyActionString(ForeignKeyAction action)
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

        public static string SqlType(TableMapping.Column p)
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
                clrType.GetTypeInfo().IsEnum)
            {
                return "integer";
            }
            if (clrType == typeof (UInt32) ||
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

        public static string GetTableName(Type info)
        {
            string tableName = info.Name;

            TableAttribute[] attrs = info.GetTypeInfo().GetAttributes<TableAttribute>().ToArray();
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

        public static string GetOldTableName(Type info)
        {
            RenameTableAttribute[] attrs = info.GetTypeInfo().GetAttributes<RenameTableAttribute>().ToArray();
            return attrs.Any() ? attrs.First().OldName : string.Empty;
        }

        public static ConflictResolution GetOnPrimaryKeyConflict(Type info)
        {
            TableAttribute[] attrs = info.GetTypeInfo().GetAttributes<TableAttribute>().ToArray();
            return attrs.Any() ? attrs.First().OnPrimaryKeyConflict : ConflictResolution.Default;
        }

        public static IList<TableMapping.Index> GetIndexes(Type info, PropertyInfo[] properties)
        {
            var indices = new List<TableMapping.Index>();

            IndexAttribute tblAtt = info.GetTypeInfo().GetAttributes<IndexAttribute>().FirstOrDefault();
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

        public static IList<TableMapping.ForeignKey> GetForeignKeys(PropertyInfo[] properties)
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

                var childProps = childTable.GetTypeInfo().GetMappableProperties();
                var childProp = childProps.SingleOrDefault(x => x.Name == att.Attribute.ChildKey);

                if (childProp == null)
                {
                    throw new SqliteException(
                        string.Format("Property {0} does not exist in type {1}.",
                                      att.Attribute.ChildKey, childTable.FullName));
                }

                key.Keys.Add(GetColumnName(att.Property), GetColumnName(childProp));
            }

            return foreignKeys;
        }

        public static string GetDefaultValue(MemberInfo info)
        {
            DefaultAttribute[] attrs = info.GetAttributes<DefaultAttribute>().ToArray();
            return attrs.Any() ? attrs.First().Value : null;
        }

        public static Collation GetCollation(MemberInfo info)
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

        public static int GetMaxStringLength(MemberInfo info)
        {
            MaxLengthAttribute[] attrs = info.GetAttributes<MaxLengthAttribute>().ToArray();
            int maxLength = attrs.Any()
                                ? attrs.First().Length
                                : DefaultMaxStringLength;
            return maxLength;
        }

        public static Type GetColumnType(PropertyInfo prop)
        {
            Type nullableType = Nullable.GetUnderlyingType(prop.PropertyType);
            var type = nullableType ?? prop.PropertyType;

            DataConverterAttribute[] attrs = prop.GetAttributes<DataConverterAttribute>().ToArray();
            if (attrs.Any())
            {
                type = attrs.First().StorageType;
            }
            else if (type.GetTypeInfo().IsEnum)
            {
                var attribute = prop.GetAttributes<EnumAffinityAttribute>().FirstOrDefault();
                type = attribute == null ? typeof (int) : attribute.Type;
            }

            return type;
        }
    }
}