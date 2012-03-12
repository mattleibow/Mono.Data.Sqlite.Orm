﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public static class OrmHelper
    {
        public const int DefaultMaxStringLength = 140;

        public static string GetForeignKeyActionString(ForeignKeyAction action)
        {
            var result = string.Empty;

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
            if (clrType == typeof (String))
            {
                int len = p.MaxStringLength;
                return "varchar(" + len + ")";
            }
            if (clrType == typeof (DateTime))
            {
                return "datetime";
            }
            if (clrType == typeof (byte[]))
            {
                return "blob";
            }
            if (clrType == typeof (Guid))
            {
                return "varchar(30)";
            }

            throw new NotSupportedException("Don't know about " + clrType);
        }

        public static string GetTableName(MemberInfo info)
        {
            var tableName = info.Name;

            var attrs = info.GetAttributes<TableAttribute>().ToArray();
            if (attrs.Any())
            {
                var attributeTableName = attrs.First().Name;
                if (!string.IsNullOrEmpty(attributeTableName))
                {
                    tableName = attributeTableName;
                }
            }

            return tableName;
        }

        public static string GetOldTableName(MemberInfo info)
        {
            var attrs = info.GetAttributes<RenameTableAttribute>().ToArray();
            return attrs.Any() ? attrs.First().OldName : string.Empty;
        }

        public static ConflictResolution GetOnPrimaryKeyConflict(MemberInfo info)
        {
            var attrs = info.GetAttributes<TableAttribute>().ToArray();
            return attrs.Any() ? attrs.First().OnPrimaryKeyConflict : ConflictResolution.Default;
        }

        public static IList<TableMapping.Index> GetIndexes(MemberInfo info, PropertyInfo[] properties)
        {
            var indices = new List<TableMapping.Index>();

            var tblAtt = info.GetAttributes<IndexAttribute>().FirstOrDefault();
            if (tblAtt != null)
            {
                if (indices.Any(i => i.IndexName == tblAtt.Name))
                {
                    throw new NotSupportedException("Only one index attribute per index allowed on a table class.");
                }
                
                indices.Add(new TableMapping.Index(tblAtt.Name) {Unique = tblAtt.Unique});
            }

            foreach (var prop in properties)
            {
                foreach (var att in prop.GetAttributes<IndexedAttribute>())
                {
                    var index = indices.FirstOrDefault(x => x.IndexName == att.Name);
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
                                                 Order = att.Order
                                             })
                             orderby attribute.Order
                             select attribute;

            foreach (var att in attributes)
            {
                var key = foreignKeys.FirstOrDefault(fk => fk.Name == att.Attribute.Name);
                if (key == null)
                {
                    key = new TableMapping.ForeignKey
                              {
                                  Name = att.Attribute.Name,
                                  ChildTable = GetTableName(att.Attribute.ChildTable),
                                  OnDelete = att.Attribute.OnDeleteAction,
                                  OnUpdate = att.Attribute.OnUpdateAction,
                                  NullMatch = att.Attribute.NullMatch,
                                  Deferred = att.Attribute.Deferred
                              };
                    foreignKeys.Add(key);
                }

                key.Keys.Add(GetColumnName(att.Property), att.Attribute.ChildKey);
            }

            return foreignKeys;
        }

        public static string GetDefaultValue(MemberInfo info)
        {
            var attrs = info.GetAttributes<DefaultAttribute>().ToArray();
            return attrs.Any() ? attrs.First().Value : null;
        }

        public static Collation GetCollation(MemberInfo info)
        {
            var attrs = info.GetAttributes<CollationAttribute>().ToArray();
            return attrs.Any() ? attrs.First().Collation : Collation.Default;
        }

        public static string GetColumnName(MemberInfo info)
        {
            var name = info.Name;

            var attrs = info.GetAttributes<ColumnAttribute>().ToArray();
            if (attrs.Any())
            {
                var attributeName = attrs.First().Name;
                if (!string.IsNullOrEmpty(attributeName))
                {
                    name = attributeName;
                }
            }

            return name;
        }

        public static int GetMaxStringLength(MemberInfo info)
        {
            var attrs = info.GetAttributes<MaxLengthAttribute>().ToArray();
            var maxLength = attrs.Any()
                                ? attrs.First().Length
                                : DefaultMaxStringLength;
            return maxLength;
        }
    }
}