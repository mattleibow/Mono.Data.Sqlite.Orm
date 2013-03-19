using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Mono.Data.Sqlite.Orm.ComponentModel;

// dictionary[TypeMapping, Extras, IsDefaultValueInsert]  = DbCommad
using InsertCommandDictionary = System.Collections.Concurrent.ConcurrentDictionary<System.Tuple<Mono.Data.Sqlite.Orm.TableMapping, Mono.Data.Sqlite.Orm.ComponentModel.ConflictResolution, bool>, System.Data.Common.DbCommand>;

// dictionary[TypeMapping]  = DbCommad
using MappingCommandDictionary = System.Collections.Concurrent.ConcurrentDictionary<System.Tuple<Mono.Data.Sqlite.Orm.TableMapping, Mono.Data.Sqlite.Orm.ComponentModel.ConflictResolution>, System.Data.Common.DbCommand>;

// dictionary[Sql] = DbCommad
using CommandDictionary = System.Collections.Concurrent.ConcurrentDictionary<System.String, System.Data.Common.DbCommand>;

namespace Mono.Data.Sqlite.Orm
{
    public class QueryCache : IDisposable
    {
        private readonly MappingCommandDictionary updateCommands;
        private readonly InsertCommandDictionary insertCommands;
        private readonly CommandDictionary cachedCommands;

        public QueryCache(SqliteConnection connection)
        {
            this.updateCommands = new MappingCommandDictionary();
            this.insertCommands = new InsertCommandDictionary();
            this.cachedCommands = new CommandDictionary();

            this.Connection = connection;
        }

        private SqliteConnection Connection { get; set; }

        /// <summary>
        /// Add the specified arguments to the specified command.
        /// </summary>
        /// <param name="cmd">
        /// The command that will receive the arguments.
        /// </param>
        /// <param name="args">The arguments to add.</param>
        private void AddCommandParameters(DbCommand cmd, params object[] args)
        {
            if (args != null)
            {
                int count = cmd.Parameters.Count;

                for (int i = 0; i < args.Length; i++)
                {
                    object value = args[i];

                    if (value != null)
                    {
                        if (value is Guid)
                        {
                            value = value.ToString();
                        }
                    }

                    if (count > i)
                    {
                        cmd.Parameters[i].Value = value;
                    }
                    else
                    {
                        DbParameter param = cmd.CreateParameter();
                        param.Value = value;
                        cmd.Parameters.Add(param);
                    }
                }
            }
        }

        /// <summary>
        ///   Creates a new SQLiteCommand given the command text with arguments. Place a '?'
        ///   in the command text for each of the arguments.
        /// </summary>
        /// <param name = "cmdText">
        ///   The fully escaped SQL.
        /// </param>
        /// <param name = "args">
        ///   Arguments to substitute for the occurrences of '?' in the command text.
        /// </param>
        /// <returns>
        ///   A <see cref = "DbCommand" />
        /// </returns>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public DbCommand CreateCommand(string cmdText, params object[] args)
        {
            bool created = false;
            var command = cachedCommands.GetOrAdd(cmdText, sql =>
                {
                    created = true;
                    DbCommand cmd = this.Connection.CreateCommand();
                    cmd.CommandText = sql;
                    return cmd;
                });

            if (SqliteSessionBase.Trace)
            {
                Debug.WriteLine("Creating command: {0}", created);
            }

            AddCommandParameters(command, args);

            return command;
        }

        public DbCommand GetInsertCommand(TableMapping mapping, ConflictResolution extra, object[] args)
        {
            var key = new Tuple<TableMapping, ConflictResolution, bool>(mapping, extra, false);
            bool created = false;
            var command = insertCommands.GetOrAdd(key, tuple =>
                {
                    created = true;
                    DbCommand cmd = this.Connection.CreateCommand();
                    cmd.CommandText = mapping.GetInsertSql(extra, (args == null));
                    return cmd;
                });

            if (SqliteSessionBase.Trace)
            {
                Debug.WriteLine("Creating insert command: {0}", created);
            }

            if (args != null)
            {
                AddCommandParameters(command, args);
            }

            return command;
        }

        public DbCommand GetUpdateCommand(TableMapping mapping, ConflictResolution extra, object[] args)
        {
            var key = new Tuple<TableMapping, ConflictResolution>(mapping, extra);
            bool created = false;
            var command = updateCommands.GetOrAdd(key, tuple =>
                {
                    created = true;
                    DbCommand cmd = this.Connection.CreateCommand();
                    cmd.CommandText = mapping.GetUpdateSql(extra);
                    return cmd;
                });

            if (SqliteSessionBase.Trace)
            {
                Debug.WriteLine("Creating update command: {0}", created);
            }

            if (args != null)
            {
                AddCommandParameters(command, args);
            }

            return command;
        }

        public void Dispose()
        {
            foreach (var cmd in insertCommands)
            {
                cmd.Value.Dispose();
            }
            insertCommands.Clear();
            foreach (var cmd in cachedCommands)
            {
                cmd.Value.Dispose();
            }
            cachedCommands.Clear();
        }
    }
}

#if SILVERLIGHT

namespace System
{
    public class Tuple<TItem1, TItem2>
    {
        public TItem1 Item1 { get; set; }
        public TItem2 Item2 { get; set; }

        public Tuple(TItem1 item1, TItem2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<TItem1, TItem2>;
            if (other == null)
                return false;
            return Item1.Equals(other.Item1) && Item2.Equals(other.Item2);
        }
    }
    public class Tuple<TItem1, TItem2, TItem3>
    {
        public TItem1 Item1 { get; set; }
        public TItem2 Item2 { get; set; }
        public TItem3 Item3 { get; set; }

        public Tuple(TItem1 item1, TItem2 item2, TItem3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<TItem1, TItem2, TItem3>;
            if (other == null)
                return false;
            return Item1.Equals(other.Item1) && Item2.Equals(other.Item2) && Item3.Equals(other.Item3);
        }
    }
}

namespace System.Collections.Concurrent
{
    public class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> func)
        {
            if (!_dictionary.ContainsKey(key))
            {
                _dictionary[key] = func(key);
            }

            return _dictionary[key];
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Clear()
        {
            _dictionary.Clear();
        }
    }
}

#endif