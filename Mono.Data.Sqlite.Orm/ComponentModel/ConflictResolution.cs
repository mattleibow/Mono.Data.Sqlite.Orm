namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    public enum ConflictResolution
    {
        Abort,
        Rollback,
        Fail,
        Ignore,
        Replace,

        Default = Abort
    }
}