using System.Data.Common;

namespace SecretSaver.Infrastructure.Persistence;

public interface IDatabaseProvider
{
    DbConnection CreateConnection();
    string CreateSchemaSql { get; }
}
