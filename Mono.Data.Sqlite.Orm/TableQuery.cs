using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Mono.Data.Sqlite.Orm
{
    public partial class TableQuery<T> : TableQueryBase<T>
        where T : new()
    {
        private TableQuery(SqliteSession conn, TableMappingBase table)
        {
            Session = conn;
            Table = table;
        }

        public TableQuery(SqliteSession conn)
        {
            Session = conn;
            Table = Session.GetMapping<T>();
        }

        protected override TableQueryBase<T> CloneInternal()
        {
            return new TableQuery<T>(Session, this.Table);
        }

        public SqliteSession Session { get; private set; }

        private DbCommand GenerateCommand(string selectionList)
        {
            List<object> args;
            StringBuilder sb;
            this.GenerateCommandSql(selectionList, out args, out sb);

            return Session.CreateCommand(sb.ToString(), args.ToArray());
        }

        public override IEnumerator<T> GetEnumerator()
        {
            using (var command = this.GetSelectCommand())
            {
                return _deferred
                           ? Session.ExecuteDeferredQuery<T>(Table, command).GetEnumerator()
                           : Session.ExecuteQuery<T>(Table, command).GetEnumerator();
            }
        }

        public override int Count()
        {
            DbCommand command;

            if (this._distinct)
            {
                command = this.GetSelectCommand();
                command.CommandText = string.Format("SELECT COUNT(*) FROM ({0})", command.CommandText);
            }
            else
            {
                command = GenerateCommand("COUNT(*)");
            }

            return Session.ExecuteScalar<int>(command);
        }

        private DbCommand GetSelectCommand()
        {
            IEnumerable<string> columns;
            if (this._withColumns == null || this._withColumns.Count <= 0)
            {
                columns = this.Table.Columns.Select(c => SqliteWriter.Quote(c.Name));
            }
            else
            {
                columns = this._withColumns.Select(c => SqliteWriter.Quote(c.ColumnName));
            }
            return this.GenerateCommand(string.Join(", ", columns));
        }
    }
}