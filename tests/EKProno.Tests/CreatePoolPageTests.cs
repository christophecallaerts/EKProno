using System.Net;
using EKProno.Storage;

namespace EKProno.Tests;

/// <summary>
/// End-to-end over HTTP: authentication, antiforgery, model binding, the store and the
/// rendered pages together.
/// </summary>
public class CreatePoolPageTests : IClassFixture<EkPronoWebApplicationFactory>
{
    private readonly EkPronoWebApplicationFactory _factory;

    public CreatePoolPageTests(EkPronoWebApplicationFactory factory) => _factory = factory;

    private IDataStore Store => _factory.Store;

    [Fact] // SC-007, FR-010, EC-7
    public async Task An_anonymous_visitor_is_sent_to_sign_in_instead_of_the_create_page()
    {
        var client = _factory.CreateTrackingClient();

        var response = await client.GetAsync("/Pools/Create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/SignIn", response.Headers.Location!.OriginalString);
    }

    [Fact] // SC-007, EC-7
    public async Task An_anonymous_post_to_the_create_endpoint_creates_no_pool()
    {
        var client = _factory.CreateTrackingClient();
        var poolsBefore = Store.Query(data => data.Pools.Count);

        var response = await client.PostAsync("/Pools/Create", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["PoolName"] = "Sneaky EK",
                ["UseExistingTournament"] = "false",
                ["TournamentName"] = "Europees Kampioenschap",
                ["TournamentEdition"] = "2028",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/SignIn", response.Headers.Location!.OriginalString);
        Assert.Equal(poolsBefore, Store.Query(data => data.Pools.Count));
        Assert.DoesNotContain(Store.Query(data => data.Pools), p => p.Name == "Sneaky EK");
    }

    [Fact] // SC-001, SC-003, FR-015
    public async Task An_organiser_creates_a_pool_and_lands_on_the_schedule_screen()
    {
        var client = _factory.CreateTrackingClient();
        await EkPronoWebApplicationFactory.SignInAsync(client, "carla@example.com", "Carla");

        var response = await client.PostFormAsync("/Pools/Create", new Dictionary<string, string>
        {
            ["PoolName"] = "Carla EK",
            ["UseExistingTournament"] = "false",
            ["TournamentName"] = "Europees Kampioenschap",
            ["TournamentEdition"] = "2028",
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.OriginalString;
        Assert.StartsWith("/Pools/Schedule/", location);

        var pool = Assert.Single(Store.Query(data => data.Pools.Where(p => p.Name == "Carla EK").ToList()));
        Assert.Contains(pool.Id.ToString(), location);
        Assert.False(string.IsNullOrWhiteSpace(pool.JoinToken));
        Assert.Contains(Store.Query(data => data.Players), p => p.PoolId == pool.Id && p.IsOrganiser);
        Assert.Contains(Store.Query(data => data.ScoringRules), r => r.PoolId == pool.Id);

        // The schedule screen opens for its organiser.
        var schedule = await client.GetAsync(location);
        schedule.EnsureSuccessStatusCode();
        Assert.Contains("Carla EK", await schedule.Content.ReadAsStringAsync());
    }

    [Fact] // SC-004
    public async Task A_blank_pool_name_is_rejected_with_a_validation_message()
    {
        var client = _factory.CreateTrackingClient();
        await EkPronoWebApplicationFactory.SignInAsync(client, "dirk@example.com", "Dirk");
        var poolsBefore = Store.Query(data => data.Pools.Count);

        var response = await client.PostFormAsync("/Pools/Create", new Dictionary<string, string>
        {
            ["PoolName"] = "   ",
            ["UseExistingTournament"] = "false",
            ["TournamentName"] = "Europees Kampioenschap",
            ["TournamentEdition"] = "2028",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A pool name is required.", await response.Content.ReadAsStringAsync());
        Assert.Equal(poolsBefore, Store.Query(data => data.Pools.Count));
    }

    [Fact] // SC-005, EC-2
    public async Task A_duplicate_pool_name_is_rejected_with_a_validation_message()
    {
        var client = _factory.CreateTrackingClient();
        await EkPronoWebApplicationFactory.SignInAsync(client, "els@example.com", "Els");

        var fields = new Dictionary<string, string>
        {
            ["PoolName"] = "Els' EK",
            ["UseExistingTournament"] = "false",
            ["TournamentName"] = "Europees Kampioenschap",
            ["TournamentEdition"] = "2028",
        };

        var first = await client.PostFormAsync("/Pools/Create", fields);
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        var second = await client.PostFormAsync("/Pools/Create", new Dictionary<string, string>(fields)
        {
            ["PoolName"] = "ELS' EK",
            ["TournamentEdition"] = "2030",
        });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Contains("You already have a pool with that name.", await second.Content.ReadAsStringAsync());
        Assert.Single(Store.Query(data => data.Pools.Where(p => p.Name == "Els' EK").ToList()));
    }
}
