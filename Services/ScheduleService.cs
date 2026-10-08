using EKProno.Domain;
using EKProno.Storage;

namespace EKProno.Services;

/// <summary>The pool and the tournament a schedule screen is working on.</summary>
public sealed record ScheduleContext(Guid PoolId, string PoolName, Guid TournamentId, string TournamentName);

/// <summary>A team as the team list shows it; <paramref name="MatchCount"/> drives FR-004/FR-005.</summary>
public sealed record TeamView(Guid Id, string Name, int MatchCount);

/// <summary>A fixture as the schedule shows it (FR-017).</summary>
public sealed record MatchView(
    Guid Id,
    Stage Stage,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTimeOffset KickoffAt,
    DateTime LocalKickoff,
    bool HasKickedOff);

/// <summary>The whole schedule in one read, plus the per-stage counts of FR-018.</summary>
public sealed record ScheduleView(
    ScheduleContext Context,
    TimeZoneInfo TimeZone,
    IReadOnlyList<MatchView> Matches,
    IReadOnlyList<TeamView> Teams)
{
    public int CountFor(Stage stage) => Matches.Count(m => m.Stage == stage);
}

/// <summary>
/// What the organiser types into the add/edit match form. The kickoff arrives as a local
/// wall-clock moment plus the zone it was read in; the service turns it into an instant
/// (FR-009).
/// </summary>
public sealed record MatchInput(Guid HomeTeamId, Guid AwayTeamId, DateTime? LocalKickoff, string TimeZoneId, Stage Stage);

/// <summary>
/// A saved fixture together with the non-blocking warning the organiser should still see
/// (FR-010, EC-7).
/// </summary>
public sealed record SavedMatch(Match Match, string? Warning);

/// <summary>
/// Everything spec 002 asks of a tournament's teams and fixtures. Like
/// <see cref="PoolService"/> it returns <see cref="Result{TValue}"/> rather than throwing,
/// so pages can render field-level messages.
/// </summary>
public sealed class ScheduleService(IDataStore store, TimeProvider clock)
{
    /// <summary>
    /// NFR-003 / SC-013: a pool whose tournament the caller does not own reads as missing.
    /// Every public method below funnels through this.
    /// </summary>
    public ScheduleContext? GetContext(Guid ownerId, Guid poolId) =>
        store.Query(data => ResolveContext(data, ownerId, poolId));

    /// <summary>FR-001..FR-005: the tournament's teams, alphabetically, with their usage count.</summary>
    public IReadOnlyList<TeamView>? ListTeams(Guid ownerId, Guid poolId) =>
        store.Query(data => ResolveContext(data, ownerId, poolId) is { } context
            ? TeamViews(data, context.TournamentId)
            : null);

    /// <summary>FR-001, FR-002.</summary>
    public Result<Team> AddTeam(Guid ownerId, Guid poolId, string? name)
    {
        var teamName = (name ?? string.Empty).Trim();

        if (ValidateTeamName(teamName) is { } nameError)
        {
            return Result<Team>.Failure(nameError.Field, nameError.Message);
        }

        return store.Mutate(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return Result<Team>.Failure(string.Empty, "That pool does not exist.");
            }

            if (HasTeamNamed(data, context.TournamentId, teamName, exceptTeamId: null))
            {
                return Result<Team>.Failure("Name", "That team is already in the tournament.");
            }

            var team = new Team
            {
                TournamentId = context.TournamentId,
                Name = teamName,
                CreatedAt = clock.GetUtcNow(),
            };

            data.Teams.Add(team);
            return Result<Team>.Success(team);
        },
        shouldCommit: result => result.Succeeded);
    }

    /// <summary>
    /// FR-003: renaming propagates for free — matches reference the team by id, never by name.
    /// </summary>
    public Result<Team> RenameTeam(Guid ownerId, Guid poolId, Guid teamId, string? name)
    {
        var teamName = (name ?? string.Empty).Trim();

        if (ValidateTeamName(teamName) is { } nameError)
        {
            return Result<Team>.Failure(nameError.Field, nameError.Message);
        }

        return store.Mutate(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return Result<Team>.Failure(string.Empty, "That pool does not exist.");
            }

            var team = data.Teams.SingleOrDefault(t => t.Id == teamId && t.TournamentId == context.TournamentId);
            if (team is null)
            {
                return Result<Team>.Failure(string.Empty, "That team does not exist.");
            }

            // EC-8: a clash with any other team is a duplicate; keeping your own name is a no-op.
            if (HasTeamNamed(data, context.TournamentId, teamName, exceptTeamId: teamId))
            {
                return Result<Team>.Failure("Name", "That team is already in the tournament.");
            }

            team.Name = teamName;
            return Result<Team>.Success(team);
        },
        shouldCommit: result => result.Succeeded);
    }

    /// <summary>FR-004, FR-005: only an unused team goes; a used one names its blockers.</summary>
    public Result<Team> RemoveTeam(Guid ownerId, Guid poolId, Guid teamId) =>
        store.Mutate(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return Result<Team>.Failure(string.Empty, "That pool does not exist.");
            }

            var team = data.Teams.SingleOrDefault(t => t.Id == teamId && t.TournamentId == context.TournamentId);
            if (team is null)
            {
                return Result<Team>.Failure(string.Empty, "That team does not exist.");
            }

            var blocking = data.Matches
                .Where(m => m.TournamentId == context.TournamentId &&
                            (m.HomeTeamId == teamId || m.AwayTeamId == teamId))
                .Select(m => Describe(data, m))
                .ToList();

            if (blocking.Count > 0)
            {
                // FR-005 / SC-011: say exactly which fixtures have to go first.
                return Result<Team>.Failure(
                    string.Empty,
                    $"{team.Name} still plays in {blocking.Count} match{(blocking.Count == 1 ? string.Empty : "es")}. " +
                    $"Remove {string.Join(", ", blocking)} first.");
            }

            data.Teams.Remove(team);
            return Result<Team>.Success(team);
        },
        shouldCommit: result => result.Succeeded);

    /// <summary>FR-017, FR-018: the whole schedule, read in <paramref name="timeZone"/>.</summary>
    public ScheduleView? GetSchedule(Guid ownerId, Guid poolId, TimeZoneInfo timeZone) =>
        store.Query(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return null;
            }

            var now = clock.GetUtcNow();

            var matches = data.Matches
                .Where(m => m.TournamentId == context.TournamentId)
                .Select(m => new MatchView(
                    m.Id,
                    m.Stage,
                    m.HomeTeamId,
                    TeamName(data, m.HomeTeamId),
                    m.AwayTeamId,
                    TeamName(data, m.AwayTeamId),
                    m.KickoffAt,
                    TimeZoneInfo.ConvertTime(m.KickoffAt, timeZone).DateTime,
                    m.HasKickedOff(now)))
                // EC-4: simultaneous fixtures order deterministically, by stage then team name.
                .OrderBy(m => m.KickoffAt)
                .ThenBy(m => (int)m.Stage)
                .ThenBy(m => m.HomeTeamName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.AwayTeamName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new ScheduleView(context, timeZone, matches, TeamViews(data, context.TournamentId));
        });

    /// <summary>A single fixture, for the edit form.</summary>
    public Match? GetMatch(Guid ownerId, Guid poolId, Guid matchId) =>
        store.Query(data => ResolveContext(data, ownerId, poolId) is { } context
            ? data.Matches.SingleOrDefault(m => m.Id == matchId && m.TournamentId == context.TournamentId)
            : null);

    /// <summary>FR-006..FR-012.</summary>
    public Result<SavedMatch> AddMatch(Guid ownerId, Guid poolId, MatchInput input)
    {
        var kickoff = ResolveKickoff(input);
        if (kickoff.Failed)
        {
            return Result<SavedMatch>.Failure(kickoff.Error.Field, kickoff.Error.Message);
        }

        return store.Mutate(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return Result<SavedMatch>.Failure(string.Empty, "That pool does not exist.");
            }

            if (ValidateTeams(data, context.TournamentId, input, exceptMatchId: null) is { } teamError)
            {
                return Result<SavedMatch>.Failure(teamError.Field, teamError.Message);
            }

            var match = new Match
            {
                TournamentId = context.TournamentId,
                HomeTeamId = input.HomeTeamId,
                AwayTeamId = input.AwayTeamId,
                KickoffAt = kickoff.Value,
                Stage = input.Stage,
                CreatedAt = clock.GetUtcNow(),
            };

            data.Matches.Add(match);
            return Result<SavedMatch>.Success(new SavedMatch(match, PastKickoffWarning(kickoff.Value)));
        },
        shouldCommit: result => result.Succeeded);
    }

    /// <summary>FR-013, FR-014.</summary>
    public Result<SavedMatch> EditMatch(Guid ownerId, Guid poolId, Guid matchId, MatchInput input)
    {
        var kickoff = ResolveKickoff(input);
        if (kickoff.Failed)
        {
            return Result<SavedMatch>.Failure(kickoff.Error.Field, kickoff.Error.Message);
        }

        return store.Mutate(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return Result<SavedMatch>.Failure(string.Empty, "That pool does not exist.");
            }

            var match = data.Matches.SingleOrDefault(m => m.Id == matchId && m.TournamentId == context.TournamentId);
            if (match is null)
            {
                return Result<SavedMatch>.Failure(string.Empty, "That match does not exist.");
            }

            // FR-014 / EC-6: re-checked here, against the clock at submit time rather than
            // at the time the form was rendered.
            if (match.HasKickedOff(clock.GetUtcNow()))
            {
                return Result<SavedMatch>.Failure(
                    string.Empty, "A match that has kicked off can no longer be edited.");
            }

            if (ValidateTeams(data, context.TournamentId, input, exceptMatchId: matchId) is { } teamError)
            {
                // EC-5: rejected, and the stored match keeps its previous values because
                // nothing has been assigned yet.
                return Result<SavedMatch>.Failure(teamError.Field, teamError.Message);
            }

            match.HomeTeamId = input.HomeTeamId;
            match.AwayTeamId = input.AwayTeamId;
            match.KickoffAt = kickoff.Value;
            match.Stage = input.Stage;

            return Result<SavedMatch>.Success(new SavedMatch(match, PastKickoffWarning(kickoff.Value)));
        },
        shouldCommit: result => result.Succeeded);
    }

    /// <summary>FR-015, FR-016.</summary>
    public Result<Match> DeleteMatch(Guid ownerId, Guid poolId, Guid matchId) =>
        store.Mutate(data =>
        {
            if (ResolveContext(data, ownerId, poolId) is not { } context)
            {
                return Result<Match>.Failure(string.Empty, "That pool does not exist.");
            }

            var match = data.Matches.SingleOrDefault(m => m.Id == matchId && m.TournamentId == context.TournamentId);
            if (match is null)
            {
                return Result<Match>.Failure(string.Empty, "That match does not exist.");
            }

            if (DeletionBlocker(data, match) is { } blocker)
            {
                return Result<Match>.Failure(string.Empty, blocker);
            }

            data.Matches.Remove(match);
            return Result<Match>.Success(match);
        },
        shouldCommit: result => result.Succeeded);

    /// <summary>
    /// FR-015: why this match may not be deleted, or <c>null</c> when it may.
    /// Predictions (story #006) and results (story #008) are not modelled yet, so for now
    /// nothing blocks a deletion. The check lives here so those stories only extend this
    /// method rather than hunting for the rule.
    /// </summary>
    private static string? DeletionBlocker(EkPronoData data, Match match) => null;

    private static ScheduleContext? ResolveContext(EkPronoData data, Guid ownerId, Guid poolId)
    {
        var pool = data.Pools.SingleOrDefault(p => p.Id == poolId);
        if (pool is null)
        {
            return null;
        }

        // FR-019: authorised against the tournament's owner, not merely the pool's organiser.
        var tournament = data.Tournaments.SingleOrDefault(t => t.Id == pool.TournamentId && t.OwnerId == ownerId);

        return tournament is null
            ? null
            : new ScheduleContext(pool.Id, pool.Name, tournament.Id, tournament.FullName);
    }

    private static IReadOnlyList<TeamView> TeamViews(EkPronoData data, Guid tournamentId) =>
        data.Teams
            .Where(t => t.TournamentId == tournamentId)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new TeamView(
                t.Id,
                t.Name,
                data.Matches.Count(m => m.HomeTeamId == t.Id || m.AwayTeamId == t.Id)))
            .ToList();

    private static Error? ValidateTeamName(string trimmedName)
    {
        if (string.IsNullOrEmpty(trimmedName))
        {
            return new Error("Name", "A team name is required.");
        }

        return trimmedName.Length > Team.NameMaxLength
            ? new Error("Name", $"A team name may be at most {Team.NameMaxLength} characters.")
            : null;
    }

    private static bool HasTeamNamed(EkPronoData data, Guid tournamentId, string name, Guid? exceptTeamId) =>
        data.Teams.Any(t =>
            t.TournamentId == tournamentId &&
            t.Id != exceptTeamId &&
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>FR-006, FR-007, FR-011 — everything about a fixture except its kickoff.</summary>
    private static Error? ValidateTeams(EkPronoData data, Guid tournamentId, MatchInput input, Guid? exceptMatchId)
    {
        if (!Enum.IsDefined(input.Stage))
        {
            return new Error("Stage", "Pick a stage for this match.");
        }

        // FR-006 and the "teams stay inside their tournament" invariant in one check.
        var home = data.Teams.SingleOrDefault(t => t.Id == input.HomeTeamId && t.TournamentId == tournamentId);
        if (home is null)
        {
            return new Error("HomeTeamId", "Pick a home team from this tournament.");
        }

        var away = data.Teams.SingleOrDefault(t => t.Id == input.AwayTeamId && t.TournamentId == tournamentId);
        if (away is null)
        {
            return new Error("AwayTeamId", "Pick an away team from this tournament.");
        }

        if (home.Id == away.Id)
        {
            return new Error("AwayTeamId", "A team cannot play itself.");
        }

        // FR-011 / SC-006: the stage is part of the key, so the same pair may meet again later.
        var duplicate = data.Matches.Any(m =>
            m.TournamentId == tournamentId &&
            m.Id != exceptMatchId &&
            m.HomeTeamId == home.Id &&
            m.AwayTeamId == away.Id &&
            m.Stage == input.Stage);

        return duplicate
            ? new Error(string.Empty, $"{home.Name} versus {away.Name} already exists in the {input.Stage.DisplayName().ToLowerInvariant()}.")
            : null;
    }

    /// <summary>
    /// FR-009 / NFR-005: reads the wall-clock moment in the organiser's zone and pins it to
    /// an absolute instant. EC-3: a local time that does not exist or happens twice is
    /// rejected rather than guessed at.
    /// </summary>
    private static Result<DateTimeOffset> ResolveKickoff(MatchInput input)
    {
        if (input.LocalKickoff is not { } local)
        {
            return Result<DateTimeOffset>.Failure("LocalKickoff", "A kickoff date and time is required.");
        }

        var timeZone = ResolveTimeZone(input.TimeZoneId);

        if (timeZone.IsInvalidTime(local))
        {
            return Result<DateTimeOffset>.Failure(
                "LocalKickoff",
                $"{local:yyyy-MM-dd HH:mm} does not exist in {timeZone.DisplayName} — the clocks move forward then. Pick another time.");
        }

        if (timeZone.IsAmbiguousTime(local))
        {
            return Result<DateTimeOffset>.Failure(
                "LocalKickoff",
                $"{local:yyyy-MM-dd HH:mm} happens twice in {timeZone.DisplayName} — the clocks move back then. Pick another time.");
        }

        var offset = timeZone.GetUtcOffset(local);
        return Result<DateTimeOffset>.Success(
            new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset).ToUniversalTime());
    }

    /// <summary>An unknown or missing zone id falls back to the server's own zone.</summary>
    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Local;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone) ? timeZone : TimeZoneInfo.Local;
    }

    /// <summary>FR-010, EC-2, EC-7: warn about a locked fixture, never block it.</summary>
    private string? PastKickoffWarning(DateTimeOffset kickoff) =>
        kickoff <= clock.GetUtcNow()
            ? "That kickoff is in the past, so predictions for this match are already locked."
            : null;

    private static string TeamName(EkPronoData data, Guid teamId) =>
        data.Teams.SingleOrDefault(t => t.Id == teamId)?.Name ?? "(unknown team)";

    private static string Describe(EkPronoData data, Match match) =>
        $"{TeamName(data, match.HomeTeamId)} versus {TeamName(data, match.AwayTeamId)} ({match.Stage.DisplayName().ToLowerInvariant()})";
}
