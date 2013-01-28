using System;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Data.Sqlite.Orm.ComponentModel;

namespace Mono.Data.Sqlite.Orm
{
    public class TableMapping : TableMappingBase
    {
        private DbCommand _insertCommand;
        private DbCommand _selectCommand;

        public TableMapping(Type type) 
            : base(type)
        {
        }

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand GetInsertCommand(DbConnection connection, ConflictResolution extra, bool withDefaults)
        {
            if (_insertCommand != null && (_insertExtra != extra || _insertDefaults != withDefaults))
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
                _insertDefaults = withDefaults;

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

        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal DbCommand GetSelectCommand(DbConnection connection)
        {
            if (_selectCommand == null)
            {
                if (SqliteSession.Trace)
                {
                    Debug.WriteLine(string.Format("Creating Select command for {0} ({1})", TableName, MappedType));
                }

                _selectCommand = connection.CreateCommand();
                _selectCommand.CommandText = this.GetSelectSql();
                _selectCommand.Prepare();
            }

            return _selectCommand;
        }

        public override void Dispose()
        {
            if (_insertCommand != null)
            {
                if (SqliteSession.Trace)
                {
                    Debug.WriteLine(string.Format("Destroying Insert command for {0} ({1})", TableName, MappedType));
                }

                _insertCommand.Dispose();
            }

            if (_selectCommand != null)
            {
                if (SqliteSession.Trace)
                {
                    Debug.WriteLine(string.Format("Destroying Select command for {0} ({1})", TableName, MappedType));
                }

                _selectCommand.Dispose();
            }
        }
    }
}