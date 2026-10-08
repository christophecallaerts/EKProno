using System.Net;

namespace EKProno.Tests;

public class PoolListPageTests : IClassFixture<EkPronoWebApplicationFactory>
{
    private readonly EkPronoWebApplicationFactory _factory;

    public PoolListPageTests(EkPronoWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> SignedInAs(string email, string displayName)
    {
        var client = _factory.CreateTrackingClient();
        await EkPronoWebApplicationFactory.SignInAsync(client, email, displayName);
        return client;
    }

    private static async Task CreatePool(HttpClient client, string poolName, string edition)
    {
        var response = await client.PostFormAsync("/Pools/Create", new Dictionary<string, string>
        {
            ["PoolName"] = poolName,
            ["UseExistingTournament"] = "false",
            ["TournamentName"] = "Europees Kampioenschap",
            ["TournamentEdition"] = edition,
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact] // FR-012
    public async Task A_first_time_organiser_is_prompted_to_create_a_pool()
    {
        var client = await SignedInAs("fien@example.com", "Fien");

        var response = await client.GetAsync("/Pools");
        response.EnsureSuccessStatusCode();

        Assert.Contains("Create your first pool", await response.Content.ReadAsStringAsync());
    }

    [Fact] // SC-008
    public async Task An_organiser_sees_their_pools_and_not_somebody_elses()
    {
        var ann = await SignedInAs("ann@example.com", "Ann");
        await CreatePool(ann, "Vrienden EK", "2028");
        await CreatePool(ann, "Collega's EK", "2030");

        var bob = await SignedInAs("bob@example.com", "Bob");
        await CreatePool(bob, "Familie EK", "2028");

        var html = await (await ann.GetAsync("/Pools")).Content.ReadAsStringAsync();

        Assert.Contains("Vrienden EK", html);
        Assert.Contains("Collega&#x27;s EK", html);
        Assert.DoesNotContain("Familie EK", html);
    }

    [Fact] // SC-009, FR-013
    public async Task An_organiser_renames_their_own_pool()
    {
        var client = await SignedInAs("greet@example.com", "Greet");
        await CreatePool(client, "Greet's EK", "2028");
        var pool = _factory.Store.Query(data => data.Pools.Single(p => p.Name == "Greet's EK"));

        var response = await client.PostFormAsync($"/Pools/Rename/{pool.Id}", new Dictionary<string, string>
        {
            ["Name"] = "Greet's EK 2028",
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var renamed = _factory.Store.Query(data => data.Pools.Single(p => p.Id == pool.Id));
        Assert.Equal("Greet's EK 2028", renamed.Name);
        Assert.Equal(pool.TournamentId, renamed.TournamentId);
        Assert.Equal(pool.JoinToken, renamed.JoinToken);
    }

    [Fact] // NFR-003, SC-004
    public async Task Somebody_elses_pool_is_not_found()
    {
        var hugo = await SignedInAs("hugo@example.com", "Hugo");
        await CreatePool(hugo, "Hugo's EK", "2028");
        var pool = _factory.Store.Query(data => data.Pools.Single(p => p.Name == "Hugo's EK"));

        var ines = await SignedInAs("ines@example.com", "Ines");

        var get = await ines.GetAsync($"/Pools/Rename/{pool.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        var post = await ines.PostAsync($"/Pools/Rename/{pool.Id}", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Name"] = "Ines' EK" }));
        Assert.NotEqual(HttpStatusCode.Redirect, post.StatusCode);

        Assert.Equal("Hugo's EK", _factory.Store.Query(data => data.Pools.Single(p => p.Id == pool.Id).Name));
    }
}
