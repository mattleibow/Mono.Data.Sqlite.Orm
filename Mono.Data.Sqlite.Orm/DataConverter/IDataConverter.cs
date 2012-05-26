namespace Mono.Data.Sqlite.Orm.DataConverter
{
    using System;
        
    public interface IDataConverter
    {
        object Convert(object value, Type targetType, object parameter);
        object ConvertBack(object value, Type targetType, object parameter);
    }
}
