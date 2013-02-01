namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    public enum ForeignKeyAction
    {
        NoAction,
        Restrict,
        SetNull,
        SetDefault,
        Cascade,

        Default = NoAction
    }
}