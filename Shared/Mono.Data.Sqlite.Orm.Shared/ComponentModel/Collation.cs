namespace Mono.Data.Sqlite.Orm.ComponentModel
{
    /// <summary>
    /// Used by the COLLATE clause following each column name and defines a 
    /// collating sequence used for text entries in that column. 
    /// </summary>
    /// <remarks>
    /// The default collating sequence is the collating sequence defined for 
    /// that column in the CREATE TABLE statement. 
    /// Or if no collating sequence is otherwise defined, the built-in BINARY 
    /// collating sequence is used.
    /// </remarks>
    public enum Collation
    {
        /// <summary>
        /// Compares string data using memcmp(), regardless of text encoding.
        /// </summary>
        Binary,

        /// <summary>
        /// The same as binary, except the 26 upper case characters of ASCII 
        /// are folded to their lower case equivalents before the comparison 
        /// is performed.
        /// </summary>
        /// <remarks> 
        /// Note: only ASCII characters are case folded. 
        /// SQLite does not attempt to do full UTF case folding due to the
        /// size of the tables required.
        /// </remarks>
        NoCase,

        /// <summary>
        /// The same as binary, except that trailing space characters are 
        /// ignored.
        /// </summary>
        RTrim,

        /// <summary>
        /// Use the <see cref="Collation.Binary"/> collating sequence.
        /// </summary>
        Default = Binary
    }
}