namespace Mono.Data.Sqlite.Orm
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Linq.Expressions;

    public partial class TableQuery<T>
    {
        public Task<int> CountAsync()
        {
            return Task<int>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.Count();
                        }
                    });
        }

        public Task<T> ElementAtAsync(int index)
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.ElementAt(index);
                        }
                    });
        }

        public Task<T> ElementAtOrDefaultAsync(int index)
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.ElementAtOrDefault(index);
                        }
                    });
        }

        public Task<T> FirstAsync()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.First();
                        }
                    });
        }

        public Task<T> FirstAsync(Expression<Func<T, bool>> predicate)
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.First(predicate);
                        }
                    });
        }

        public Task<T> FirstOrDefaultAsync()
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.FirstOrDefault();
                        }
                    });
        }

        public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return Task<T>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.FirstOrDefault(predicate);
                        }
                    });
        }

        public Task<List<T>> ToListAsync()
        {
            return Task<List<T>>.Factory.StartNew(
                () =>
                    {
                        using (this.Session.Lock())
                        {
                            return this.ToList();
                        }
                    });
        }
    }
}
