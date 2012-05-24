namespace Mono.Data.Sqlite.Orm
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

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
