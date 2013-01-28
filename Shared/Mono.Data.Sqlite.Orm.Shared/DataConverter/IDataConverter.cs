namespace Mono.Data.Sqlite.Orm.DataConverter
{
    using System;

    using ComponentModel;

    /// <summary>
    /// Provides a way to apply custom storage types to a property/column.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to associate a data converter with a binding, create a 
    /// class that implements the IDataConverter interface and then implement 
    /// the Convert and ConvertBack methods. 
    /// </para>
    /// <para>
    /// The Convert and ConvertBack methods also have a parameter called 
    /// <i>parameter</i> so that you can use the same instance of the converter
    /// with different parameters. 
    /// For example, you can write a formatting converter that produces 
    /// different formats of data based on the input parameter that you use. 
    /// You can use the <see cref="DataConverterAttribute.Parameter"/> of the 
    /// <see cref="DataConverterAttribute"/> class to pass a parameter as an 
    /// argument into the Convert and ConvertBack methods.
    /// </para>
    /// </remarks>
    /// <example>
    /// This example saves a Color structure as a slash separated string: 
    /// "RRR/GGG/BBB". 
    /// It can then read the string and parse it back into a Color object.
    /// <code>
    /// public class ColorConverter : IDataConverter
    /// {
    ///     public object Convert(object value, Type targetType, object parameter)
    ///     {
    ///         Color color = Color.Black;
    ///         if (value is Color)
    ///         {
    ///             color = (Color)value;
    ///         }
    ///         return string.Join("/", color.R, color.G, color.B);
    ///     }
    ///     public object ConvertBack(object value, Type targetType, object parameter)
    ///     {
    ///         try
    ///         {
    ///             var parts = value.ToString().Split('/');
    ///             return Color.FromArgb(byte.Parse(parts[0]), 
    ///                                   byte.Parse(parts[1]),
    ///                                   byte.Parse(parts[2]));
    ///         }
    ///         catch
    ///         {
    ///             return Color.Black;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IDataConverter
    {
        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        object Convert(object value, Type targetType, object parameter);
        /// <summary>
        /// Converts the back.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns></returns>
        /// <remarks></remarks>
        object ConvertBack(object value, Type targetType, object parameter);
    }
}
