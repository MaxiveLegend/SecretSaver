using SecretSaver.Domain;

namespace SecretSaver.Application;

public class SecretService(ISecretRepository secretRepository)
{
    public async Task<Guid> CreateAsync(string value, TimeSpan expiresIn, int maxViews)
    {
        var secret = Secret.Create(value, expiresIn, maxViews);
        await secretRepository.AddAsync(secret);
        return secret.Id;
    }

    public async Task<string?> RevealAsync(Guid id)
    {
        var secret = await secretRepository.GetByIdAsync(id);
        
        if (secret == null) return null;
        
        if (secret.IsUsedUp)
        {
            await secretRepository.DeleteAsync(secret);
            return null;
        }

        await ProcessView(secret);

        return secret.Value;
    }

    private async Task ProcessView(Secret secret)
    {
        secret.RegisterView();

        if (secret.IsUsedUp)
        {
            await secretRepository.DeleteAsync(secret);
            return;
        }
        
        await secretRepository.UpdateAsync(secret);
    }
}