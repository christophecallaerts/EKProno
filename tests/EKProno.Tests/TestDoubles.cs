using EKProno.Domain;
using EKProno.Services;
using EKProno.Storage;

namespace EKProno.Tests;

/// <summary>A clock that stands still unless a test moves it.</summary>
public sealed class TestClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public TestClock() : this(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero))
    {
    }

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

/// <summary>Hands out the tokens a test lines up, then falls back to a counter.</summary>
public sealed class StubJoinTokenGenerator(params string[] tokens) : IJoinTokenGenerator
{
    private readonly Queue<string> _queued = new(tokens);

    public int GenerateCount { get; private set; }

    public string Generate()
    {
        GenerateCount++;
        return _queued.Count > 0 ? _queued.Dequeue() : $"token-{GenerateCount}";
    }
}

/// <summary>A generator that always returns the same token, to force a collision (EC-6).</summary>
public sealed class ConstantJoinTokenGenerator(string token) : IJoinTokenGenerator
{
    public int GenerateCount { get; private set; }

    public string Generate()
    {
        GenerateCount++;
        return token;
    }
}

/// <summary>
/// Wires a <see cref="PoolService"/> over an in-memory store and exposes the raw data so
/// tests can assert on what was — and was not — persisted.
/// </summary>
public sealed class PoolTestContext
{
    public PoolTestContext(IJoinTokenGenerator? joinTokens = null)
    {
        Store = new JsonFileDataStore(filePath: null);
        Clock = new TestClock();
        JoinTokens = joinTokens ?? new StubJoinTokenGenerator();
        Pools = new PoolService(Store, JoinTokens, Clock);
        Accounts = new UserAccountService(Store, Clock);
    }

    public JsonFileDataStore Store { get; }
    public TestClock Clock { get; }
    public IJoinTokenGenerator JoinTokens { get; }
    public PoolService Pools { get; }
    public UserAccountService Accounts { get; }

    public EkPronoData Data => Store.Query(data => data);

    public UserAccount GivenAccount(string email, string displayName = "")
    {
        var result = Accounts.SignIn(email, displayName);
        Assert.True(result.Succeeded);
        return result.Value;
    }

    public Pool GivenPool(Guid organiserId, string poolName, string tournamentName = "Europees Kampioenschap",
        string edition = "2028")
    {
        var existing = Pools.ListTournamentsOwnedBy(organiserId)
            .SingleOrDefault(t =>
                string.Equals(t.Name, tournamentName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.Edition, edition, StringComparison.OrdinalIgnoreCase));

        TournamentChoice choice = existing is null
            ? new TournamentChoice.New(tournamentName, edition)
            : new TournamentChoice.Existing(existing.Id);

        var result = Pools.CreatePool(organiserId, new CreatePoolRequest(poolName, choice));
        Assert.True(result.Succeeded);
        return result.Value;
    }
}
