namespace EKProno.Domain;

/// <summary>
/// The football competition a pool follows. Owned by the organiser who entered it —
/// there is no shared catalogue (spec 001 §9.2).
/// </summary>
public sealed class Tournament
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public const int NameMaxLength = 100;
    public const int EditionMaxLength = 20;

    public string FullName => $"{Name} {Edition}";
}
