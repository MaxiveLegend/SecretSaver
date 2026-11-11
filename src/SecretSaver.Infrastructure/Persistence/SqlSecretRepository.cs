using System.Data.Common;
using System.Globalization;
using SecretSaver.Application;
using SecretSaver.Domain;

namespace SecretSaver.Infrastructure.Persistence;

internal sealed class SqlSecretRepository(IDatabaseProvider database) : ISecretRepository
{
    public Task AddAsync(Secret secret) => ExecuteAsync(
        """
        INSERT INTO Secrets (Id, Value, ExpiryDate, Views, MaxViews)
        VALUES (@id, @value, @expiryDate, @views, @maxViews)
        """,
        ("@id", secret.Id.ToString()),
        ("@value", secret.Value),
        ("@expiryDate", secret.ExpiryDate.ToString("O", CultureInfo.InvariantCulture)),
        ("@views", secret.Views),
        ("@maxViews", secret.MaxViews));

    public async Task<Secret?> GetByIdAsync(Guid id)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Value, ExpiryDate, Views, MaxViews FROM Secrets WHERE Id = @id";
        command.AddParameter("@id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public Task UpdateAsync(Secret secret) => ExecuteAsync(
        "UPDATE Secrets SET Views = @views WHERE Id = @id",
        ("@id", secret.Id.ToString()),
        ("@views", secret.Views));

    public Task DeleteAsync(Secret secret) => ExecuteAsync(
        "DELETE FROM Secrets WHERE Id = @id",
        ("@id", secret.Id.ToString()));

    private static Secret Map(DbDataReader reader) => Secret.Restore(
        id: Guid.Parse(reader.GetString(0)),
        value: reader.GetString(1),
        expiryDate: DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        views: reader.GetInt32(3),
        maxViews: reader.GetInt32(4));

    private async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
            command.AddParameter(name, value);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<DbConnection> OpenConnectionAsync()
    {
        var connection = database.CreateConnection();
        await connection.OpenAsync();
        return connection;
    }
}
