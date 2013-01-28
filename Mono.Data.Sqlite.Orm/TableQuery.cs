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
            return new TableQuery<T>(Session as SqliteSession, this.Table);
        }
    }
}