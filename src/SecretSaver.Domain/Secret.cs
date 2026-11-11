namespace SecretSaver.Domain;

public class Secret
{
    public Guid Id { get; }
    public string Value { get; }
    public DateTime ExpiryDate { get; }
    public int Views { get; private set; }
    public int MaxViews { get; }

    private Secret(Guid id, string value, DateTime expiryDate, int views, int maxViews)
    {
        if (id == Guid.Empty) throw new ArgumentException("A secret needs an id.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxViews);
        ArgumentOutOfRangeException.ThrowIfNegative(views);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(views, maxViews);

        Id = id;
        Value = value;
        ExpiryDate = expiryDate;
        Views = views;
        MaxViews = maxViews;
    }

    public static Secret Create(string value, TimeSpan expiresIn, int maxViews)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(expiresIn, TimeSpan.Zero);
        return new Secret(Guid.NewGuid(), value, DateTime.UtcNow.Add(expiresIn), 0, maxViews);
    }

    public static Secret Restore(Guid id, string value, DateTime expiryDate, int views, int maxViews)
    {
        return new Secret(id, value, expiryDate, views, maxViews);
    }

    public void RegisterView()
    {
        if (Views >= MaxViews)
            throw new InvalidOperationException("This secret has no views left.");

        Views++;
    }

    public bool IsUsedUp => ExpiryDate < DateTime.UtcNow || Views >= MaxViews;
}
