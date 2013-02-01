namespace Mono.Data.Sqlite.Orm
{
    public interface ITrackConnection
    {
        SqliteSessionBase Connection { get; set; }
    }
}