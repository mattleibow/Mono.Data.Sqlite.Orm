namespace Mono.Data.Sqlite.Orm
{
    public partial class TableQuery<T> : TableQueryBase<T>
        where T : new()
    {
        public TableQuery(SqliteSession session)
            : base(session)
        {
        }

        public TableQuery(SqliteSessionBase session, TableMapping table)
            : base(session, table)
        {
        }

        protected override TableQueryBase<T> CloneInternal()
        {
            return new TableQuery<T>(Session, this.Table);
        }
    }
}