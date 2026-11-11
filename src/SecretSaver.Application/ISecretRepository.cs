using SecretSaver.Domain;

namespace SecretSaver.Application;

public interface ISecretRepository
{
    Task AddAsync(Secret secret);
    Task<Secret?> GetByIdAsync(Guid id);
    Task UpdateAsync(Secret secret);
    Task DeleteAsync(Secret secret);
}