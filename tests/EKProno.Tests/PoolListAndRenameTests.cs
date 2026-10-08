namespace EKProno.Tests;

/// <summary>
/// Spec 001 US-002 (finding your pools) and US-003 (renaming one).
/// </summary>
public class PoolListAndRenameTests
{
    [Fact] // SC-008, FR-011
    public void An_organiser_sees_only_their_own_pools()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var bob = context.GivenAccount("bob@example.com", "Bob");
        context.GivenPool(ann.Id, "Vrienden EK");
        context.GivenPool(bob.Id, "Familie EK");

        var annsPools = context.Pools.ListPoolsOwnedBy(ann.Id);

        Assert.Equal(["Vrienden EK"], annsPools.Select(p => p.Name));
    }

    [Fact] // FR-011
    public void The_pool_list_is_most_recently_created_first_and_names_the_tournament()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        context.GivenPool(ann.Id, "Vrienden EK", "Europees Kampioenschap", "2028");
        context.Clock.Advance(TimeSpan.FromMinutes(1));
        context.GivenPool(ann.Id, "Collega's EK", "Wereldbeker", "2030");

        var pools = context.Pools.ListPoolsOwnedBy(ann.Id);

        Assert.Equal(["Collega's EK", "Vrienden EK"], pools.Select(p => p.Name));
        Assert.Equal("Wereldbeker 2030", pools[0].TournamentName);
        Assert.Equal("Europees Kampioenschap 2028", pools[1].TournamentName);
    }

    [Fact] // FR-012
    public void A_first_time_organiser_owns_no_pools()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");

        Assert.Empty(context.Pools.ListPoolsOwnedBy(ann.Id));
    }

    [Fact] // SC-009, FR-013
    public void Renaming_a_pool_leaves_everything_else_untouched()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var pool = context.GivenPool(ann.Id, "Vrienden EK");

        var result = context.Pools.RenamePool(ann.Id, pool.Id, "Vrienden EK 2028");

        Assert.True(result.Succeeded);
        var renamed = Assert.Single(context.Data.Pools);
        Assert.Equal("Vrienden EK 2028", renamed.Name);
        Assert.Equal(pool.TournamentId, renamed.TournamentId);
        Assert.Equal(pool.JoinToken, renamed.JoinToken);
        Assert.Equal(pool.OrganiserId, renamed.OrganiserId);
        Assert.Single(context.Data.Players);
        Assert.Single(context.Data.ScoringRules);
    }

    [Fact] // EC-10
    public void Renaming_a_pool_to_the_name_it_already_has_is_accepted()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var pool = context.GivenPool(ann.Id, "Vrienden EK");

        var result = context.Pools.RenamePool(ann.Id, pool.Id, "  Vrienden EK  ");

        Assert.True(result.Succeeded);
        Assert.Equal("Vrienden EK", Assert.Single(context.Data.Pools).Name);
    }

    [Fact] // FR-013, FR-003
    public void Renaming_a_pool_onto_another_of_my_pools_is_rejected()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        context.GivenPool(ann.Id, "Vrienden EK");
        var second = context.GivenPool(ann.Id, "Collega's EK");

        var result = context.Pools.RenamePool(ann.Id, second.Id, "VRIENDEN EK");

        Assert.True(result.Failed);
        Assert.Equal("Collega's EK", context.Data.Pools.Single(p => p.Id == second.Id).Name);
    }

    [Theory] // FR-013, FR-002
    [InlineData("")]
    [InlineData("   ")]
    public void Renaming_a_pool_to_a_blank_name_is_rejected(string newName)
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var pool = context.GivenPool(ann.Id, "Vrienden EK");

        var result = context.Pools.RenamePool(ann.Id, pool.Id, newName);

        Assert.True(result.Failed);
        Assert.Equal("Vrienden EK", Assert.Single(context.Data.Pools).Name);
    }

    [Fact] // NFR-003, SC-004
    public void A_pool_cannot_be_read_or_renamed_by_anyone_but_its_organiser()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var bob = context.GivenAccount("bob@example.com", "Bob");
        var pool = context.GivenPool(ann.Id, "Vrienden EK");

        Assert.Null(context.Pools.GetPoolOwnedBy(bob.Id, pool.Id));

        var result = context.Pools.RenamePool(bob.Id, pool.Id, "Bob's EK");

        Assert.True(result.Failed);
        Assert.Equal("Vrienden EK", Assert.Single(context.Data.Pools).Name);
    }

    [Fact] // SC-010, FR-014
    public void The_tournament_a_pool_follows_survives_a_rename()
    {
        var context = new PoolTestContext();
        var ann = context.GivenAccount("ann@example.com", "Ann");
        var pool = context.GivenPool(ann.Id, "Vrienden EK", "Europees Kampioenschap", "2028");
        context.GivenPool(ann.Id, "Collega's EK", "Wereldbeker", "2030");

        context.Pools.RenamePool(ann.Id, pool.Id, "Vrienden EK 2028");

        // There is deliberately no operation that changes a pool's tournament: rename is the
        // only mutation spec 001 exposes, and it leaves TournamentId alone.
        Assert.Equal(pool.TournamentId, context.Data.Pools.Single(p => p.Id == pool.Id).TournamentId);
    }
}
