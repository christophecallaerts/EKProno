namespace EKProno.Domain;

/// <summary>
/// A single fixture: two teams of one tournament, a kickoff moment and a stage
/// (spec 002 §5.1). <see cref="KickoffAt"/> is the instant predictions lock against
/// (story #007), so it is always stored as an absolute instant in UTC.
/// </summary>
public sealed class Match
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Immutable after creation.</summary>
    public Guid TournamentId { get; set; }

    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    public DateTimeOffset KickoffAt { get; set; }
    public Stage Stage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// A started match is frozen: its teams and kickoff may no longer change (FR-014).
    /// </summary>
    public bool HasKickedOff(DateTimeOffset now) => KickoffAt <= now;
}
