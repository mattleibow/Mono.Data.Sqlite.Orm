namespace Mono.Data.Sqlite.Orm
{
    public interface ITrackConnection
    {
        SqliteSession Connection { get; set; }
    }
}