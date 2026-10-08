using EKProno.Domain;
using EKProno.Storage;

namespace EKProno.Tests;

public class JsonFileDataStoreTests
{
    [Fact]
    public void A_throwing_mutation_leaves_the_store_untouched()
    {
        var store = new JsonFileDataStore(filePath: null);

        Assert.Throws<InvalidOperationException>(() => store.Mutate<object?>(data =>
        {
            data.Pools.Add(new Pool { Name = "Half written" });
            throw new InvalidOperationException("boom");
        }));

        Assert.Empty(store.Query(data => data.Pools));
    }

    [Fact]
    public void A_mutation_the_caller_rejects_is_discarded()
    {
        var store = new JsonFileDataStore(filePath: null);

        var kept = store.Mutate(
            data =>
            {
                data.Pools.Add(new Pool { Name = "Half written" });
                return false;
            },
            shouldCommit: committed => committed);

        Assert.False(kept);
        Assert.Empty(store.Query(data => data.Pools));
    }

    [Fact]
    public void Mutating_the_snapshot_a_query_hands_out_does_not_change_the_store()
    {
        var store = new JsonFileDataStore(filePath: null);
        store.Mutate(data =>
        {
            data.Pools.Add(new Pool { Name = "Vrienden EK" });
            return true;
        });

        var snapshot = store.Query(data => data);
        snapshot.Pools.Single().Name = "Tampered";

        Assert.Equal("Vrienden EK", store.Query(data => data.Pools.Single().Name));
    }

    [Fact]
    public void Data_written_to_a_file_is_read_back_by_a_new_store()
    {
        var directory = Directory.CreateTempSubdirectory("ekprono-tests");
        var path = Path.Combine(directory.FullName, "nested", "ekprono.json");

        try
        {
            var poolId = Guid.NewGuid();
            var first = new JsonFileDataStore(path);
            first.Mutate(data =>
            {
                data.Pools.Add(new Pool
                {
                    Id = poolId,
                    Name = "Vrienden EK",
                    JoinToken = "a-token",
                });
                return true;
            });

            var second = new JsonFileDataStore(path);
            var pool = Assert.Single(second.Query(data => data.Pools));

            Assert.Equal(poolId, pool.Id);
            Assert.Equal("Vrienden EK", pool.Name);
            Assert.Equal("a-token", pool.JoinToken);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
