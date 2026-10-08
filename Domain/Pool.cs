namespace EKProno.Domain;

/// <summary>
/// A prediction pool for one tournament (spec 001 §5.1).
/// </summary>
public sealed class Pool
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid OrganiserId { get; set; }

    /// <summary>Immutable after creation — see spec 001 FR-014.</summary>
    public Guid TournamentId { get; set; }

    public string JoinToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public const int NameMaxLength = 100;
}
