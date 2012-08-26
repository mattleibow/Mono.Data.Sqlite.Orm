namespace Mono.Data.Sqlite.Orm
{
    using System;
    using System.Data;

    public static class SqliteExtensions
    {
        private static readonly Random Random = new Random();

        public static bool Matches(this string column, string match)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves a point in a transaction using the given savepoint name. 
        /// </summary>
        /// <param name="savepoint">
        /// The savepoint name to use.
        /// </param>
        public static void CreateSavepoint(this SqliteTransaction transaction, string savepoint)
        {
            EnsureInProgress(transaction);

            using (var sqliteCommand = transaction.Connection.CreateCommand())
            {
                sqliteCommand.CommandText = "SAVEPOINT " + savepoint;
                sqliteCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Saves a point in a transaction using a random name.
        /// </summary>
        /// <returns>
        /// The random savepoint name that was used.
        /// </returns>
        public static string CreateSavepoint(this SqliteTransaction transaction)
        {
            var savepoint = "SAVEPOINT_" + Random.Next() + "_" + DateTime.UtcNow.Ticks;

            transaction.CreateSavepoint(savepoint);

            return savepoint;
        }

        /// <summary>
        /// Reverts the state of the database back to what it was just after 
        /// the corresponding savepoint. This does not cancel the transaction, 
        /// but all intervening savepoints are canceled.
        /// </summary>
        /// <param name="savepoint">The savepoint name to rollback to.</param>
        public static void RollbackSavepoint(this SqliteTransaction transaction, string savepoint)
        {
            EnsureInProgress(transaction);

            using (var sqliteCommand = transaction.Connection.CreateCommand())
            {
                sqliteCommand.CommandText = "ROLLBACK TO " + savepoint;
                sqliteCommand.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// This causes all savepoints back to and including the most recent 
        /// savepoint with a matching name to be removed from the transaction 
        /// stack. It does not cause any changes to be written to the database 
        /// file, but merely removes savepoints from the transaction stack such
        /// that it is no longer possible to rollback to those savepoints.
        /// </summary>
        /// <param name="savepoint">
        /// The savepoint name to release.
        /// </param>
        public static void ReleaseSavepoint(this SqliteTransaction transaction, string savepoint)
        {
            EnsureInProgress(transaction);

            using (var sqliteCommand = transaction.Connection.CreateCommand())
            {
                sqliteCommand.CommandText = "RELEASE " + savepoint;
                sqliteCommand.ExecuteNonQuery();
            }
        }

        private static void EnsureInProgress(SqliteTransaction transaction)
        {
            if (transaction == null || transaction.Connection == null || transaction.Connection.State != ConnectionState.Open)
            {
                throw new SqliteException("Savepoints can only be used on an open transaction");
            }
        }
    }
}