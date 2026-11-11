using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace SecretSaver.Infrastructure.Persistence.Sqlite;

internal sealed class SqliteDatabaseProvider(string connectionString) : IDatabaseProvider
{
    public DbConnection CreateConnection() => new SqliteConnection(connectionString);

    public string CreateSchemaSql => """
        CREATE TABLE IF NOT EXISTS Secrets (
            Id         TEXT    PRIMARY KEY,
            Value      TEXT    NOT NULL,
            ExpiryDate TEXT    NOT NULL,
            Views      INTEGER NOT NULL,
            MaxViews   INTEGER NOT NULL
        )
        """;
}
