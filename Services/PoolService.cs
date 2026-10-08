using EKProno.Domain;
using EKProno.Storage;

namespace EKProno.Services;

/// <summary>Either an existing tournament of the organiser's, or a brand new one (FR-004).</summary>
public abstract record TournamentChoice
{
    public sealed record Existing(Guid TournamentId) : TournamentChoice;

    public sealed record New(string Name, string Edition) : TournamentChoice;
}

public sealed record CreatePoolRequest(string PoolName, TournamentChoice Tournament);

/// <summary>A pool as the organiser's list shows it (FR-011).</summary>
public sealed record PoolSummary(Guid Id, string Name, string TournamentName, DateTimeOffset CreatedAt);

/// <summary>
/// Everything spec 001 asks of a pool: creating it, listing the organiser's pools and
/// renaming one.
/// </summary>
public sealed class PoolService(IDataStore store, IJoinTokenGenerator joinTokens, TimeProvider clock)
{
    /// <summary>EC-6: how many token collisions we absorb before giving up rather than reusing one.</summary>
    private const int JoinTokenAttempts = 5;

    /// <summary>
    /// Creates a pool together with its scoring rules and its organiser player, as one
    /// all-or-nothing write (FR-001, FR-006..FR-009, NFR-005).
    /// </summary>
    public Result<Pool> CreatePool(Guid organiserId, CreatePoolRequest request)
    {
        var poolName = (request.PoolName ?? string.Empty).Trim();

        if (ValidatePoolName(poolName) is { } nameError)
        {
            return Result<Pool>.Failure(nameError.Field, nameError.Message);
        }

        return store.Mutate(data =>
        {
            var organiser = data.UserAccounts.SingleOrDefault(u => u.Id == organiserId);
            if (organiser is null)
            {
                // FR-010: no authenticated account, no pool.
                return Result<Pool>.Failure(string.Empty, "You need to sign in before you can create a pool.");
            }

            if (HasPoolNamed(data, organiserId, poolName, exceptPoolId: null))
            {
                return Result<Pool>.Failure(nameof(request.PoolName), "You already have a pool with that name.");
            }

            var tournament = ResolveTournament(data, organiserId, request.Tournament);
            if (tournament.Failed)
            {
                return Result<Pool>.Failure(tournament.Error.Field, tournament.Error.Message);
            }

            var token = GenerateUniqueJoinToken(data);
            if (token.Failed)
            {
                return Result<Pool>.Failure(token.Error.Field, token.Error.Message);
            }

            var now = clock.GetUtcNow();

            var pool = new Pool
            {
                Name = poolName,
                OrganiserId = organiserId,
                TournamentId = tournament.Value.Id,
                JoinToken = token.Value,
                CreatedAt = now,
            };

            data.Pools.Add(pool);
            data.ScoringRules.Add(ScoringRules.Defaults(pool.Id));
            data.Players.Add(new Player
            {
                PoolId = pool.Id,
                UserId = organiserId,
                DisplayName = organiser.DisplayNameOrEmailLocalPart(),
                IsOrganiser = true,
                JoinedAt = now,
            });

            return Result<Pool>.Success(pool);
        },
        // Nothing is kept unless the whole pool — rules and organiser player included — is built.
        shouldCommit: result => result.Succeeded);
    }

    /// <summary>FR-011: the organiser's own pools, most recently created first.</summary>
    public IReadOnlyList<PoolSummary> ListPoolsOwnedBy(Guid organiserId) =>
        store.Query(data => data.Pools
            .Where(p => p.OrganiserId == organiserId)
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Select(p => new PoolSummary(
                p.Id,
                p.Name,
                data.Tournaments.SingleOrDefault(t => t.Id == p.TournamentId)?.FullName ?? string.Empty,
                p.CreatedAt))
            .ToList());

    /// <summary>
    /// A pool, but only for its organiser. Anyone else gets nothing back rather than a
    /// "not yours" — the pool's existence is not theirs to learn (NFR-003).
    /// </summary>
    public Pool? GetPoolOwnedBy(Guid organiserId, Guid poolId) =>
        store.Query(data => data.Pools.SingleOrDefault(p => p.Id == poolId && p.OrganiserId == organiserId));

    /// <summary>FR-013: rename, under the same name rules as creation.</summary>
    public Result<Pool> RenamePool(Guid organiserId, Guid poolId, string newName)
    {
        var poolName = (newName ?? string.Empty).Trim();

        if (ValidatePoolName(poolName) is { } nameError)
        {
            return Result<Pool>.Failure(nameError.Field, nameError.Message);
        }

        return store.Mutate(data =>
        {
            var pool = data.Pools.SingleOrDefault(p => p.Id == poolId && p.OrganiserId == organiserId);
            if (pool is null)
            {
                return Result<Pool>.Failure(string.Empty, "That pool does not exist.");
            }

            // EC-10: renaming a pool to the name it already has is a no-op, not a clash.
            if (HasPoolNamed(data, organiserId, poolName, exceptPoolId: poolId))
            {
                return Result<Pool>.Failure("Name", "You already have a pool with that name.");
            }

            pool.Name = poolName;
            return Result<Pool>.Success(pool);
        },
        shouldCommit: result => result.Succeeded);
    }

    /// <summary>The tournaments the organiser may pick from when creating a pool (FR-004).</summary>
    public IReadOnlyList<Tournament> ListTournamentsOwnedBy(Guid ownerId) =>
        store.Query(data => data.Tournaments
            .Where(t => t.OwnerId == ownerId)
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Edition)
            .ToList());

    private static Error? ValidatePoolName(string trimmedName)
    {
        if (string.IsNullOrEmpty(trimmedName))
        {
            return new Error("PoolName", "A pool name is required.");
        }

        if (trimmedName.Length > Pool.NameMaxLength)
        {
            return new Error("PoolName", $"A pool name may be at most {Pool.NameMaxLength} characters.");
        }

        return null;
    }

    private static bool HasPoolNamed(EkPronoData data, Guid organiserId, string name, Guid? exceptPoolId) =>
        data.Pools.Any(p =>
            p.OrganiserId == organiserId &&
            p.Id != exceptPoolId &&
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    private Result<Tournament> ResolveTournament(EkPronoData data, Guid ownerId, TournamentChoice choice) =>
        choice switch
        {
            TournamentChoice.Existing existing => SelectTournament(data, ownerId, existing.TournamentId),
            TournamentChoice.New created => CreateTournament(data, ownerId, created),
            _ => Result<Tournament>.Failure("Tournament", "Pick a tournament for this pool."),
        };

    private static Result<Tournament> SelectTournament(EkPronoData data, Guid ownerId, Guid tournamentId)
    {
        var tournament = data.Tournaments.SingleOrDefault(t => t.Id == tournamentId && t.OwnerId == ownerId);

        // EC-4: a tournament owned by somebody else reads as missing. Saying "not yours"
        // would confirm it exists.
        return tournament is null
            ? Result<Tournament>.Failure("TournamentId", "That tournament does not exist.")
            : Result<Tournament>.Success(tournament);
    }

    private Result<Tournament> CreateTournament(EkPronoData data, Guid ownerId, TournamentChoice.New request)
    {
        var name = (request.Name ?? string.Empty).Trim();
        var edition = (request.Edition ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(name))
        {
            return Result<Tournament>.Failure("TournamentName", "A tournament name is required.");
        }

        if (name.Length > Tournament.NameMaxLength)
        {
            return Result<Tournament>.Failure(
                "TournamentName", $"A tournament name may be at most {Tournament.NameMaxLength} characters.");
        }

        if (string.IsNullOrEmpty(edition))
        {
            return Result<Tournament>.Failure("TournamentEdition", "A tournament edition is required.");
        }

        if (edition.Length > Tournament.EditionMaxLength)
        {
            return Result<Tournament>.Failure(
                "TournamentEdition", $"An edition may be at most {Tournament.EditionMaxLength} characters.");
        }

        var duplicate = data.Tournaments.Any(t =>
            t.OwnerId == ownerId &&
            string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(t.Edition, edition, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            // EC-5: steer the organiser to the tournament they already have.
            return Result<Tournament>.Failure(
                "TournamentName", "You already entered that tournament — select it from the list instead.");
        }

        var tournament = new Tournament
        {
            Name = name,
            Edition = edition,
            OwnerId = ownerId,
            CreatedAt = clock.GetUtcNow(),
        };

        data.Tournaments.Add(tournament);
        return Result<Tournament>.Success(tournament);
    }

    private Result<string> GenerateUniqueJoinToken(EkPronoData data)
    {
        for (var attempt = 0; attempt < JoinTokenAttempts; attempt++)
        {
            var candidate = joinTokens.Generate();
            if (!data.Pools.Any(p => p.JoinToken == candidate))
            {
                return Result<string>.Success(candidate);
            }
        }

        // EC-6: fail the creation rather than hand out a token that is already in use.
        return Result<string>.Failure(
            string.Empty, "Could not generate a join link for this pool. Please try again.");
    }
}
