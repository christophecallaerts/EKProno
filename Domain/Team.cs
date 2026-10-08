namespace EKProno.Domain;

/// <summary>
/// A national team or club taking part in one tournament (spec 002 §5.1). Scoped to its
/// tournament — teams are never shared between tournaments.
/// </summary>
public sealed class Team
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Immutable after creation.</summary>
    public Guid TournamentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public const int NameMaxLength = 60;
}
