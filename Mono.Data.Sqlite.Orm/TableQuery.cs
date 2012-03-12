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
    public class TableQuery<T> : IEnumerable<T>
        where T : new()
    {
        private bool _deferred;
        private int? _limit;
        private int? _offset;
        private List<Ordering> _orderBys;
        private Expression _where;

        private TableQuery(SqliteSession conn, TableMapping table)
        {
            Session = conn;
            Table = table;
        }

        public TableQuery(SqliteSession conn)
        {
            Session = conn;
            Table = Session.GetMapping<T>();
        }

        public SqliteSession Session { get; private set; }

        private TableMapping Table { get; set; }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            DbCommand command = GenerateCommand("*");

            return _deferred
                       ? Session.ExecuteDeferredQuery<T>(Table, command).GetEnumerator()
                       : Session.ExecuteQuery<T>(Table, command).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private TableQuery<T> Clone()
        {
            var q = new TableQuery<T>(Session, Table)
                        {
                            _where = _where,
                            _deferred = _deferred,
                            _limit = _limit,
                            _offset = _offset
                        };
            if (_orderBys != null)
            {
                q._orderBys = new List<Ordering>(_orderBys);
            }
            return q;
        }

        public TableQuery<T> Where(Expression<Func<T, bool>> predExpr)
        {
            if (predExpr.NodeType == ExpressionType.Lambda)
            {
                var lambda = (LambdaExpression) predExpr;
                Expression pred = lambda.Body;
                TableQuery<T> q = Clone();
                q.AddWhere(pred);
                return q;
            }

            throw new NotSupportedException("Must be a predicate");
        }

        public TableQuery<T> Take(int n)
        {
            TableQuery<T> q = Clone();
            q._limit = n;
            return q;
        }

        public TableQuery<T> Skip(int n)
        {
            TableQuery<T> q = Clone();
            q._offset = n;
            return q;
        }

        public TableQuery<T> Deferred()
        {
            TableQuery<T> q = Clone();
            q._deferred = true;
            return q;
        }

        public TableQuery<T> OrderBy<U>(Expression<Func<T, U>> orderExpr)
        {
            return AddOrderBy(orderExpr, true);
        }

        public TableQuery<T> OrderByDescending<U>(Expression<Func<T, U>> orderExpr)
        {
            return AddOrderBy(orderExpr, false);
        }

        private TableQuery<T> AddOrderBy<U>(Expression<Func<T, U>> orderExpr, bool asc)
        {
            if (orderExpr.NodeType != ExpressionType.Lambda)
            {
                throw new NotSupportedException("Must be a predicate");
            }

            var lambda = (LambdaExpression) orderExpr;
            var mem = lambda.Body as MemberExpression;

            if (mem == null || (mem.Expression.NodeType != ExpressionType.Parameter))
            {
                throw new NotSupportedException("Order By does not support: " + orderExpr);
            }

            TableQuery<T> q = Clone();
            if (q._orderBys == null)
            {
                q._orderBys = new List<Ordering>();
            }
            q._orderBys.Add(new Ordering
                                {
                                    ColumnName = OrmHelper.GetColumnName(mem.Member),
                                    Ascending = asc
                                });
            return q;
        }

        private void AddWhere(Expression pred)
        {
            _where = _where == null ? pred : Expression.AndAlso(_where, pred);
        }

        private DbCommand GenerateCommand(string selectionList)
        {
            string cmdText = "select " + selectionList + " from \"" + Table.TableName + "\"";
            var args = new List<object>();
            if (_where != null)
            {
                CompileResult w = CompileExpr(_where, args);
                cmdText += " where " + w.CommandText;
            }
            if ((_orderBys != null) && (_orderBys.Count > 0))
            {
                string t = string.Join(", ",
                                       _orderBys.Select(o => "\"" + o.ColumnName + "\"" + (o.Ascending ? "" : " desc")).
                                           ToArray());
                cmdText += " order by " + t;
            }
            if (_limit.HasValue)
            {
                cmdText += " limit " + _limit.Value;
            }
            if (_offset.HasValue)
            {
                if (!_limit.HasValue)
                {
                    cmdText += " limit -1 ";
                }
                cmdText += " offset " + _offset.Value;
            }
            return Session.CreateCommand(cmdText, args.ToArray());
        }

        private CompileResult CompileExpr(Expression expr, List<object> queryArgs)
        {
            if (expr == null)
            {
                throw new NotSupportedException("Expression is NULL");
            }

            if (expr is BinaryExpression)
            {
                var bin = (BinaryExpression) expr;

                CompileResult leftr = CompileExpr(bin.Left, queryArgs);
                CompileResult rightr = CompileExpr(bin.Right, queryArgs);

                //If either side is a parameter and is null, then handle the other side specially (for "is null"/"is not null")
                string text;
                if (leftr.CommandText == "?" && leftr.Value == null)
                    text = CompileNullBinaryExpression(bin, rightr);
                else if (rightr.CommandText == "?" && rightr.Value == null)
                    text = CompileNullBinaryExpression(bin, leftr);
                else
                    text = "(" + leftr.CommandText + " " + GetSqlName(bin) + " " + rightr.CommandText + ")";
                return new CompileResult {CommandText = text};
            }
            else if (expr.NodeType == ExpressionType.Call)
            {
                var call = (MethodCallExpression) expr;
                var args = new CompileResult[call.Arguments.Count];

                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = CompileExpr(call.Arguments[i], queryArgs);
                }

                string sqlCall = "";

                if (call.Method.Name == "Like" && args.Length == 2)
                {
                    sqlCall = "(" + args[0].CommandText + " like " + args[1].CommandText + ")";
                }
                else if (call.Method.Name == "Contains" && args.Length == 2)
                {
                    sqlCall = "(" + args[1].CommandText + " in " + args[0].CommandText + ")";
                }
                else
                {
                    sqlCall = call.Method.Name.ToLower() + "(" +
                              string.Join(",", args.Select(a => a.CommandText).ToArray()) + ")";
                }
                return new CompileResult {CommandText = sqlCall};
            }
            else if (expr.NodeType == ExpressionType.Constant)
            {
                var c = (ConstantExpression) expr;
                queryArgs.Add(c.Value);
                return new CompileResult
                           {
                               CommandText = "?",
                               Value = c.Value
                           };
            }
            else if (expr.NodeType == ExpressionType.Convert)
            {
                var u = (UnaryExpression) expr;
                Type ty = u.Type;
                CompileResult valr = CompileExpr(u.Operand, queryArgs);
                return new CompileResult
                           {
                               CommandText = valr.CommandText,
                               Value = valr.Value != null ? Convert.ChangeType(valr.Value, ty, null) : null
                           };
            }
            else if (expr.NodeType == ExpressionType.MemberAccess)
            {
                var mem = (MemberExpression) expr;

                if (mem.Expression.NodeType == ExpressionType.Parameter)
                {
                    //
                    // This is a column of our table, output just the column name
                    //
                    return new CompileResult {CommandText = "\"" + OrmHelper.GetColumnName(mem.Member) + "\""};
                }
                else
                {
                    object obj = null;
                    if (mem.Expression != null)
                    {
                        CompileResult r = CompileExpr(mem.Expression, queryArgs);
                        if (r.Value == null)
                        {
                            throw new NotSupportedException("Member access failed to compile expression");
                        }
                        if (r.CommandText == "?")
                        {
                            queryArgs.RemoveAt(queryArgs.Count - 1);
                        }
                        obj = r.Value;
                    }

                    //
                    // Get the member value
                    //
                    object val = null;

                    if (mem.Member.MemberType == MemberTypes.Property)
                    {
                        var m = (PropertyInfo) mem.Member;
                        val = m.GetValue(obj, null);
                    }
                    else if (mem.Member.MemberType == MemberTypes.Field)
                    {
                        var m = (FieldInfo) mem.Member;
                        val = m.GetValue(obj);
                    }
                    else
                    {
                        throw new NotSupportedException("MemberExpr: " + mem.Member.MemberType.ToString());
                    }

                    //
                    // Work special magic for enumerables
                    //
                    if (val != null && val is IEnumerable && !(val is string))
                    {
                        var sb = new StringBuilder();
                        sb.Append("(");
                        string head = "";
                        foreach (object a in (IEnumerable) val)
                        {
                            queryArgs.Add(a);
                            sb.Append(head);
                            sb.Append("?");
                            head = ",";
                        }
                        sb.Append(")");
                        return new CompileResult
                                   {
                                       CommandText = sb.ToString(),
                                       Value = val
                                   };
                    }
                    else
                    {
                        queryArgs.Add(val);
                        return new CompileResult
                                   {
                                       CommandText = "?",
                                       Value = val
                                   };
                    }
                }
            }
            throw new NotSupportedException("Cannot compile: " + expr.NodeType.ToString());
        }

        /// <summary>
        /// 	Compiles a BinaryExpression where one of the parameters is null.
        /// </summary>
        /// <param name = "expression"></param>
        /// <param name = "parameter">The non-null parameter</param>
        private string CompileNullBinaryExpression(BinaryExpression expression, CompileResult parameter)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Equal:
                    return "(" + parameter.CommandText + " is ?)";
                case ExpressionType.NotEqual:
                    return "(" + parameter.CommandText + " is not ?)";
                default:
                    throw new NotSupportedException("Cannot compile Null-BinaryExpression with type " +
                                                    expression.NodeType);
            }
        }

        private string GetSqlName(Expression expr)
        {
            ExpressionType n = expr.NodeType;
            if (n == ExpressionType.GreaterThan)
                return ">";
            else if (n == ExpressionType.GreaterThanOrEqual)
            {
                return ">=";
            }
            else if (n == ExpressionType.LessThan)
            {
                return "<";
            }
            else if (n == ExpressionType.LessThanOrEqual)
            {
                return "<=";
            }
            else if (n == ExpressionType.And)
            {
                return "and";
            }
            else if (n == ExpressionType.AndAlso)
            {
                return "and";
            }
            else if (n == ExpressionType.Or)
            {
                return "or";
            }
            else if (n == ExpressionType.OrElse)
            {
                return "or";
            }
            else if (n == ExpressionType.Equal)
            {
                return "=";
            }
            else if (n == ExpressionType.NotEqual)
            {
                return "!=";
            }
            else
            {
                throw new NotSupportedException("Cannot get SQL for: " + n.ToString());
            }
        }

        public int Count()
        {
            DbCommand command = GenerateCommand("count(*)");

            return Session.ExecuteScalar<int>(command);
        }

        #region Nested type: CompileResult

        private class CompileResult
        {
            public string CommandText { get; set; }

            public object Value { get; set; }
        }

        #endregion

        #region Nested type: Ordering

        private class Ordering
        {
            public string ColumnName { get; set; }

            public bool Ascending { get; set; }
        }

        #endregion
    }
}