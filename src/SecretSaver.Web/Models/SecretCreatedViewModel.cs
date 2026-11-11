namespace SecretSaver.Web.Models;

public class SecretCreatedViewModel
{
    public required string ShareUrl { get; init; }
    public required int ExpiresInHours { get; init; }
    public required int MaxViews { get; init; }
}
