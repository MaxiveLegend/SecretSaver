using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecretSaver.Application;
using SecretSaver.Infrastructure.Persistence;
using SecretSaver.Infrastructure.Persistence.Sqlite;

namespace SecretSaver.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var store = configuration.GetValue("Database:Provider", Stores.Sqlite);
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        IDatabaseProvider provider = store switch
        {
            Stores.Sqlite => new SqliteDatabaseProvider(connectionString),
            _ => throw new NotSupportedException($"Database provider '{store}' is not supported.")
        };

        services.AddSingleton(provider);
        services.AddScoped<ISecretRepository, SqlSecretRepository>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        var provider = services.GetRequiredService<IDatabaseProvider>();

        await using var connection = provider.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = provider.CreateSchemaSql;
        await command.ExecuteNonQueryAsync();
    }
}
