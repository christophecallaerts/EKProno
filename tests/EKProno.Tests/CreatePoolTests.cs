using EKProno.Domain;
using EKProno.Services;

namespace EKProno.Tests;

/// <summary>
/// Spec 001 §4 acceptance scenarios and §7 edge cases for creating a pool.
/// </summary>
public class CreatePoolTests
{
    [Fact] // SC-001
    public void Creating_a_pool_with_a_new_tournament_records_the_organiser()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New("Europees Kampioenschap", "2028")));

        Assert.True(result.Succeeded);
        Assert.Equal("Vrienden EK", result.Value.Name);
        Assert.Equal(ann.Id, result.Value.OrganiserId);

        var tournament = Assert.Single(context.Data.Tournaments);
        Assert.Equal("Europees Kampioenschap 2028", tournament.FullName);
        Assert.Equal(tournament.Id, result.Value.TournamentId);
        Assert.Equal(ann.Id, tournament.OwnerId);
    }

    [Fact] // SC-002
    public void Creating_a_second_pool_for_an_existing_tournament_does_not_duplicate_it()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var first = context.GivenPool(ann.Id, "Vrienden EK");

        var tournament = Assert.Single(context.Pools.ListTournamentsOwnedBy(ann.Id));
        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Collega's EK", new TournamentChoice.Existing(tournament.Id)));

        Assert.True(result.Succeeded);
        Assert.Equal(first.TournamentId, result.Value.TournamentId);
        Assert.Single(context.Data.Tournaments);
    }

    [Fact] // SC-003
    public void A_new_pool_has_an_organiser_player_default_rules_and_a_join_token()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var pool = context.GivenPool(ann.Id, "Vrienden EK");

        var player = Assert.Single(context.Data.Players);
        Assert.Equal(pool.Id, player.PoolId);
        Assert.Equal(ann.Id, player.UserId);
        Assert.Equal("Ann", player.DisplayName);
        Assert.True(player.IsOrganiser);

        var rules = Assert.Single(context.Data.ScoringRules);
        Assert.Equal(pool.Id, rules.PoolId);
        Assert.Equal(ScoringRules.DefaultCorrectOutcomePoints, rules.CorrectOutcomePoints);
        Assert.Equal(ScoringRules.ExactScoreBonusPoints, rules.ExactScoreBonus);

        Assert.False(string.IsNullOrWhiteSpace(pool.JoinToken));
    }

    [Theory] // SC-004, EC-1
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_pool_name_is_rejected(string poolName)
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            poolName, new TournamentChoice.New("Europees Kampioenschap", "2028")));

        Assert.True(result.Failed);
        Assert.Equal("PoolName", result.Error.Field);
        Assert.Empty(context.Data.Pools);
    }

    [Fact] // FR-002, EC-1
    public void A_pool_name_longer_than_100_characters_is_rejected()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            new string('x', 101), new TournamentChoice.New("Europees Kampioenschap", "2028")));

        Assert.True(result.Failed);
        Assert.Equal("PoolName", result.Error.Field);
        Assert.Empty(context.Data.Pools);
    }

    [Fact] // FR-002
    public void A_pool_name_is_stored_trimmed()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "  Vrienden EK  ", new TournamentChoice.New("Europees Kampioenschap", "2028")));

        Assert.True(result.Succeeded);
        Assert.Equal("Vrienden EK", result.Value.Name);
    }

    [Fact] // SC-005, EC-2
    public void A_duplicate_pool_name_for_the_same_organiser_is_rejected_case_insensitively()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        context.GivenPool(ann.Id, "Vrienden EK");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "vrienden ek", new TournamentChoice.New("Wereldbeker", "2030")));

        Assert.True(result.Failed);
        Assert.Equal("PoolName", result.Error.Field);
        Assert.Single(context.Data.Pools);
        // EC-2: the rejected attempt left no tournament behind either.
        Assert.Single(context.Data.Tournaments);
    }

    [Fact] // SC-006
    public void Two_organisers_may_use_the_same_pool_name()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var bob = context.GivenAccount("bob@example.com", "Bob");
        context.GivenPool(ann.Id, "Vrienden EK");

        var result = context.Pools.CreatePool(bob.Id, new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New("Europees Kampioenschap", "2028")));

        Assert.True(result.Succeeded);
        Assert.Equal(2, context.Data.Pools.Count);
        Assert.Single(context.Pools.ListPoolsOwnedBy(ann.Id));
        Assert.Single(context.Pools.ListPoolsOwnedBy(bob.Id));
    }

    [Fact] // SC-007, FR-010
    public void An_unknown_account_cannot_create_a_pool()
    {
        var context = new PoolTestContext();

        var result = context.Pools.CreatePool(Guid.NewGuid(), new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New("Europees Kampioenschap", "2028")));

        Assert.True(result.Failed);
        Assert.Empty(context.Data.Pools);
        Assert.Empty(context.Data.Tournaments);
    }

    [Fact] // EC-3
    public void Submitting_the_same_creation_twice_yields_exactly_one_pool()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var request = new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New("Europees Kampioenschap", "2028"));

        var first = context.Pools.CreatePool(ann.Id, request);
        var second = context.Pools.CreatePool(ann.Id, request);

        Assert.True(first.Succeeded);
        Assert.True(second.Failed);
        Assert.Single(context.Data.Pools);
        Assert.Single(context.Data.Players);
        Assert.Single(context.Data.ScoringRules);
    }

    [Fact] // EC-4
    public void A_tournament_owned_by_somebody_else_reads_as_missing()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var bob = context.GivenAccount("bob@example.com", "Bob");
        context.GivenPool(ann.Id, "Vrienden EK");
        var annsTournament = Assert.Single(context.Pools.ListTournamentsOwnedBy(ann.Id));

        var result = context.Pools.CreatePool(bob.Id, new CreatePoolRequest(
            "Collega's EK", new TournamentChoice.Existing(annsTournament.Id)));

        Assert.True(result.Failed);
        Assert.Equal("That tournament does not exist.", result.Error.Message);
        Assert.Single(context.Data.Pools);
    }

    [Fact] // EC-5, FR-005
    public void A_tournament_duplicating_name_and_edition_is_rejected()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        context.GivenPool(ann.Id, "Vrienden EK", "Europees Kampioenschap", "2028");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Collega's EK", new TournamentChoice.New("europees kampioenschap", "2028")));

        Assert.True(result.Failed);
        Assert.Equal("TournamentName", result.Error.Field);
        Assert.Single(context.Data.Tournaments);
    }

    [Fact] // FR-005
    public void Two_organisers_may_enter_the_same_tournament_independently()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var bob = context.GivenAccount("bob@example.com", "Bob");

        context.GivenPool(ann.Id, "Vrienden EK", "Europees Kampioenschap", "2028");
        context.GivenPool(bob.Id, "Collega's EK", "Europees Kampioenschap", "2028");

        Assert.Equal(2, context.Data.Tournaments.Count);
    }

    [Theory] // FR-005
    [InlineData("", "2028", "TournamentName")]
    [InlineData("   ", "2028", "TournamentName")]
    [InlineData("Europees Kampioenschap", "", "TournamentEdition")]
    [InlineData("Europees Kampioenschap", "   ", "TournamentEdition")]
    public void A_new_tournament_needs_a_name_and_an_edition(string name, string edition, string expectedField)
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New(name, edition)));

        Assert.True(result.Failed);
        Assert.Equal(expectedField, result.Error.Field);
        Assert.Empty(context.Data.Pools);
        Assert.Empty(context.Data.Tournaments);
    }

    [Fact] // FR-005
    public void A_tournament_name_or_edition_that_is_too_long_is_rejected()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var longName = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New(new string('x', 101), "2028")));
        var longEdition = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Vrienden EK", new TournamentChoice.New("Europees Kampioenschap", new string('9', 21))));

        Assert.Equal("TournamentName", longName.Error.Field);
        Assert.Equal("TournamentEdition", longEdition.Error.Field);
        Assert.Empty(context.Data.Tournaments);
    }

    [Fact] // EC-6
    public void A_join_token_collision_is_regenerated_transparently()
    {
        var context = new PoolTestContext(new StubJoinTokenGenerator("shared", "shared", "unique"));
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var first = context.GivenPool(ann.Id, "Vrienden EK");
        var second = context.GivenPool(ann.Id, "Collega's EK");

        Assert.Equal("shared", first.JoinToken);
        Assert.Equal("unique", second.JoinToken);
    }

    [Fact] // EC-6, EC-8, NFR-005
    public void An_unresolvable_token_collision_fails_the_whole_creation()
    {
        var generator = new ConstantJoinTokenGenerator("always-the-same");
        var context = new PoolTestContext(generator);
        var ann = context.GivenAccount("ann@example.com", "Ann");
        context.GivenPool(ann.Id, "Vrienden EK");

        var result = context.Pools.CreatePool(ann.Id, new CreatePoolRequest(
            "Collega's EK", new TournamentChoice.New("Wereldbeker", "2030")));

        Assert.True(result.Failed);
        // Nothing from the abandoned attempt survives: no pool, no rules, no player, and
        // crucially not the tournament that was already written before the token failed.
        Assert.Single(context.Data.Pools);
        Assert.Single(context.Data.Players);
        Assert.Single(context.Data.ScoringRules);
        Assert.Single(context.Data.Tournaments);
        Assert.DoesNotContain(context.Data.Tournaments, t => t.Name == "Wereldbeker");
    }

    [Fact] // FR-009, SC-005 (success criterion)
    public void Join_tokens_are_unique_and_url_safe()
    {
        var context = new PoolTestContext(new JoinTokenGenerator());
        var ann = context.GivenAccount("ann@example.com", "Ann");

        var tokens = Enumerable.Range(0, 25)
            .Select(i => context.GivenPool(ann.Id, $"Pool {i}").JoinToken)
            .ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
        Assert.All(tokens, token =>
        {
            Assert.DoesNotContain(token, c => c is '+' or '/' or '=');
            // 20 random bytes base64url-encoded; well over the 128 bits §5.3 requires.
            Assert.True(token.Length >= 22, $"Token '{token}' is shorter than expected.");
        });
    }

    [Fact] // EC-9, FR-007
    public void An_organiser_without_a_display_name_falls_back_to_their_email_local_part()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann.peeters@example.com", displayName: "");

        context.GivenPool(ann.Id, "Vrienden EK");

        var player = Assert.Single(context.Data.Players);
        Assert.Equal("ann.peeters", player.DisplayName);
    }
}
