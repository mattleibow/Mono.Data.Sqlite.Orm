using System.Linq;
using System.Text;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
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
                sb.Append(string.Join(", ", table.EditableColumns.Select(c => Quote(c.Name)).ToArray()));
                sb.Append(") VALUES (");
                sb.Append(string.Join(", ", Enumerable.Repeat("?", table.EditableColumns.Count).ToArray()));
                sb.Append(")");
            }

            return sb.ToString();
        }

        public static string GetSelectSql(this TableMapping table)
        {
            var sb = new StringBuilder();

            sb.Append("SELECT * FROM ");
            sb.Append(Quote(table.TableName));
            sb.Append(" WHERE ");

            bool first = true;
            foreach (var column in table.PrimaryKey.Columns)
            {
                if (!first)
                {
                    sb.AppendLine(" AND ");
                }
                sb.Append(Quote(column.Name));
                sb.Append(" = ?");
                first = false;
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
            if (!primaryKey.Name.IsNullOrWhitespace())
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
            if (!foreignKey.Name.IsNullOrWhitespace())
            {
                sb.Append("CONSTRAINT ");
                sb.Append(Quote(foreignKey.Name));
                sb.AppendLine();
            }
            sb.Append("FOREIGN KEY (");
            sb.Append(string.Join(", ", foreignKey.Keys.Keys.Select(Quote).ToArray()));
            sb.Append(")");
            sb.AppendLine();
            sb.Append("REFERENCES ");
            sb.Append(Quote(foreignKey.ChildTable));
            sb.Append(" (");
            sb.Append(string.Join(", ", foreignKey.Keys.Values.Select(Quote).ToArray()));
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

        public static string Quote(string name)
        {
            return "[" + name + "]";
        }
    }
}