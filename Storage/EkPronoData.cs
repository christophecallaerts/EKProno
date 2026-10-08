using EKProno.Domain;

namespace EKProno.Storage;

/// <summary>
/// The whole database, as a single serialisable document. Small by design: EKProno's
/// data set is a handful of pools per organiser, so one document keeps writes atomic
/// (spec 001 NFR-005) without a transaction manager.
/// </summary>
public sealed class EkPronoData
{
    public List<UserAccount> UserAccounts { get; set; } = [];
    public List<Tournament> Tournaments { get; set; } = [];
    public List<Team> Teams { get; set; } = [];
    public List<Match> Matches { get; set; } = [];
    public List<Pool> Pools { get; set; } = [];
    public List<Player> Players { get; set; } = [];
    public List<ScoringRules> ScoringRules { get; set; } = [];
}
