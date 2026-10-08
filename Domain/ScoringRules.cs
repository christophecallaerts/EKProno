namespace EKProno.Domain;

/// <summary>
/// The pool's point configuration. Created with the defaults from spec 003 (FR-002, FR-004);
/// spec 003 owns the attributes and the editing rules.
/// </summary>
public sealed class ScoringRules
{
    public const int DefaultCorrectOutcomePoints = 1;
    public const int ExactScoreBonusPoints = 1;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public int CorrectOutcomePoints { get; set; } = DefaultCorrectOutcomePoints;

    /// <summary>Fixed at 1 by spec 003 — read-only for the organiser.</summary>
    public int ExactScoreBonus { get; set; } = ExactScoreBonusPoints;

    public static ScoringRules Defaults(Guid poolId) => new()
    {
        PoolId = poolId,
        CorrectOutcomePoints = DefaultCorrectOutcomePoints,
        ExactScoreBonus = ExactScoreBonusPoints,
    };
}
