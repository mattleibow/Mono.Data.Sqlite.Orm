namespace Mono.Data.Sqlite.Orm
{
    using System;

    public static class FullTextSearchSpecialCommands
    {
        private const string OptimizeCommand = "INSERT INTO {0}({0}) VALUES ('optimize');";

        private const string RebuildCommand = "INSERT INTO {0}({0}) VALUES ('rebuild');";

        private const string IntegrityCheckCommand = "INSERT INTO {0}({0}) VALUES ('integrity-check');";

        private const string MergeXyCommand = "INSERT INTO {0}({0}) VALUES ('merge={1},{2}');";

        private const string AutoMergeCommand = "INSERT INTO {0}({0}) VALUES ('automerge={1}');";

        private const string ChangesCommand = "sqlite3_total_changes();";

        /// <summary>
        ///   The "optimize" command causes FTS3/4 to merge together all of its 
        ///   inverted index b-trees into one large and complete b-tree.
        /// </summary>
        /// <typeparam name="T">The table to optimize.</typeparam>
        /// <param name="database">The database to use.</param>
        public static int Optimize<T>(this SqliteSessionBase database)
        {
            return database.Optimize(typeof(T));
        }

        /// <summary>
        ///   The "optimize" command causes FTS3/4 to merge together all of its 
        ///   inverted index b-trees into one large and complete b-tree.
        /// </summary>
        /// <param name="database">The database to use.</param>
        /// <param name="type">The table to optimize.</param>
        public static int Optimize(this SqliteSessionBase database, Type type)
        {
            var map = database.GetMapping(type);
            return database.Execute(String.Format(OptimizeCommand, map.TableName));
        }

        /// <summary>
        ///   The "rebuild" command causes SQLite to discard the entire FTS3/4 
        ///   table and then rebuild it again from original text. 
        ///   The concept is similar to REINDEX.
        /// </summary>
        /// <typeparam name="T">The table to rebuild.</typeparam>
        /// <param name="database">The database to use.</param>
        public static int Rebuild<T>(this SqliteSessionBase database)
        {
            return database.Rebuild(typeof(T));
        }

        /// <summary>
        ///   The "rebuild" command causes SQLite to discard the entire FTS3/4 
        ///   table and then rebuild it again from original text. 
        ///   The concept is similar to REINDEX.
        /// </summary>
        /// <param name="database">The database to use.</param>
        /// <param name="type">The table to rebuild.</param>
        public static int Rebuild(this SqliteSessionBase database, Type type)
        {
            var map = database.GetMapping(type);
            return database.Execute(String.Format(RebuildCommand, map.TableName));
        }

        /// <summary>
        ///   The "integrity-check" command causes SQLite to read and verify 
        ///   the accuracy of all inverted indices in an FTS3/4 table by 
        ///   comparing those inverted indices against the original content.
        /// </summary>
        /// <typeparam name="T">The table to check.</typeparam>
        /// <param name="database">The database to use.</param>
        public static int IntegrityCheck<T>(this SqliteSessionBase database)
        {
            return database.IntegrityCheck(typeof(T));
        }

        /// <summary>
        ///   The "integrity-check" command causes SQLite to read and verify 
        ///   the accuracy of all inverted indices in an FTS3/4 table by 
        ///   comparing those inverted indices against the original content.
        /// </summary>
        /// <param name="database">The database to use.</param>
        /// <param name="type">The table to check.</param>
        public static int IntegrityCheck(this SqliteSessionBase database, Type type)
        {
            var map = database.GetMapping(type);
            return database.Execute(String.Format(IntegrityCheckCommand, map.TableName));
        }

        /// <summary>
        ///   The "merge=X,Y" command (where X and Y are integers) causes 
        ///   SQLite to do a limited amount of work toward merging the various 
        ///   inverted index b-trees of an FTS3/4 table together into one large
        ///   b-tree.
        /// </summary>
        /// <typeparam name="T">
        ///   The table on which to perform the merge.
        /// </typeparam>
        /// <param name="database">The database to use.</param>
        /// <param name="x">
        ///   The X value is the target number of "blocks" to be merged.
        ///   The value of X can be any positive integer but values on the 
        ///   order of 100 to 300 are recommended.
        /// </param>
        /// <param name="y">
        ///   The Y is the minimum number of b-tree segments on a level 
        ///   required before merging will be applied to that level.
        ///   The value of Y should be between 2 and 16 with a recommended 
        ///   value of 8.
        /// </param>
        public static int Merge<T>(this SqliteSessionBase database, int x = 100, int y = 8)
        {
            return database.Merge(typeof(T), x, y);
        }

        /// <summary>
        ///   The "merge=X,Y" command (where X and Y are integers) causes 
        ///   SQLite to do a limited amount of work toward merging the various 
        ///   inverted index b-trees of an FTS3/4 table together into one large
        ///   b-tree.
        /// </summary>
        /// <param name="database">The database to use.</param>
        /// <param name="type">The table on which to perform the merge.</param>
        /// <param name="x">
        ///   The X value is the target number of "blocks" to be merged.
        ///   The value of X can be any positive integer but values on the 
        ///   order of 100 to 300 are recommended.
        /// </param>
        /// <param name="y">
        ///   The Y is the minimum number of b-tree segments on a level 
        ///   required before merging will be applied to that level.
        ///   The value of Y should be between 2 and 16 with a recommended 
        ///   value of 8.
        /// </param>
        public static int Merge(this SqliteSessionBase database, Type type, int x = 100, int y = 8)
        {
            var map = database.GetMapping(type);
            return database.Execute(String.Format(MergeXyCommand, map.TableName, x, y));
        }

        /// <summary>
        ///   To avoid spiky INSERT performance, an application can run the 
        ///   "merge=X,Y" command periodically, possibly in an idle thread or 
        ///   idle process.
        ///   The idle thread that is running the merge commands can know when 
        ///   it is done by checking the difference in sqlite3_total_changes() 
        ///   before and after each "merge=X,Y" command and stopping the loop 
        ///   when the difference drops below two. 
        /// </summary>
        /// <typeparam name="T">
        ///   The table on which to perform the merges.
        /// </typeparam>
        /// <param name="database">The database to use.</param>
        public static int RunMergeUntilOptimal<T>(this SqliteSessionBase database)
        {
            return database.RunMergeUntilOptimal(typeof(T));
        }

        /// <summary>
        ///   To avoid spiky INSERT performance, an application can run the 
        ///   "merge=X,Y" command periodically, possibly in an idle thread or 
        ///   idle process.
        ///   The idle thread that is running the merge commands can know when 
        ///   it is done by checking the difference in sqlite3_total_changes() 
        ///   before and after each "merge=X,Y" command and stopping the loop 
        ///   when the difference drops below two. 
        /// </summary>
        /// <param name="database">The database to use.</param>
        /// <param name="type">
        ///   The table on which to perform the merges.
        /// </param>
        public static int RunMergeUntilOptimal(this SqliteSessionBase database, Type type)
        {
            int changes = 0;
            int i;
            while ((i = database.Merge(type)) >= 2)
            {
                changes += i;
            }
            return changes;
        }

        /// <summary>
        ///   The "automerge=B" command disables or enables automatic 
        ///   incremental inverted index merging for an FTS3/4 table. 
        ///   Enabling automatic incremental merge causes SQLite to do a small 
        ///   amount of inverted index merging after every INSERT operation to 
        ///   prevent spiky INSERT performance. 
        /// </summary>
        /// <typeparam name="T">
        ///   The table on which to enable/disable the merges.
        /// </typeparam>
        /// <param name="database">The database to use.</param>
        /// <param name="enable">
        ///   True to enable automatic incremental inverted index. False to 
        ///   disable.
        /// </param>
        public static int AutoMerge<T>(this SqliteSessionBase database, bool enable = false)
        {
            return database.AutoMerge(typeof(T), enable);
        }

        /// <summary>
        ///   The "automerge=B" command disables or enables automatic 
        ///   incremental inverted index merging for an FTS3/4 table. 
        ///   Enabling automatic incremental merge causes SQLite to do a small 
        ///   amount of inverted index merging after every INSERT operation to 
        ///   prevent spiky INSERT performance. 
        /// </summary>
        /// <param name="database">The database to use.</param>
        /// <param name="type">
        ///   The table on which to enable/disable the merges.
        ///  </param>
        /// <param name="enable">
        ///   True to enable automatic incremental inverted index. False to 
        ///   disable.
        /// </param>
        public static int AutoMerge(this SqliteSessionBase database, Type type, bool enable = false)
        {
            var map = database.GetMapping(type);
            return database.Execute(String.Format(AutoMergeCommand, map.TableName, enable ? 1 : 0));
        }
    }
}