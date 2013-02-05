namespace Mono.Data.Sqlite.Orm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Linq.Expressions;

    public static class TableQueryExtensions
    {
        public static Task<int> CountAsync<T>(this TableQuery<T> tableQuery) where T : new()
        {
            return Task<int>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.Count();
                        }
                    });
        }

        public static Task<T> ElementAtAsync<T>(this TableQuery<T> tableQuery, int index) where T : new()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.ElementAt(index);
                        }
                    });
        }

        public static Task<T> ElementAtOrDefaultAsync<T>(this TableQuery<T> tableQuery, int index) where T : new()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.ElementAtOrDefault(index);
                        }
                    });
        }

        public static Task<T> FirstAsync<T>(this TableQuery<T> tableQuery) where T : new()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.First();
                        }
                    });
        }

        public static Task<T> FirstAsync<T>(this TableQuery<T> tableQuery, Expression<Func<T, bool>> predicate) where T : new()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.First(predicate);
                        }
                    });
        }

        public static Task<T> FirstOrDefaultAsync<T>(this TableQuery<T> tableQuery) where T : new()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.FirstOrDefault();
                        }
                    });
        }

        public static Task<T> FirstOrDefaultAsync<T>(this TableQuery<T> tableQuery, Expression<Func<T, bool>> predicate) where T : new()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.FirstOrDefault(predicate);
                        }
                    });
        }

        public static Task<List<T>> ToListAsync<T>(this TableQuery<T> tableQuery) where T : new()
        {
            return Task<List<T>>.Factory.StartNew(
                () =>
                    {
                        using (tableQuery.Session.Lock())
                        {
                            return tableQuery.ToList();
                        }
                    });
        }
    }
}
