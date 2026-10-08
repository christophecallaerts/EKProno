namespace EKProno.Storage;

/// <summary>
/// The persistence seam. Everything above this interface works with plain domain objects,
/// so replacing the JSON document with a real database is a single DI registration.
/// </summary>
public interface IDataStore
{
    /// <summary>Reads from a snapshot. The instance handed to <paramref name="query"/> is never mutated.</summary>
    TResult Query<TResult>(Func<EkPronoData, TResult> query);

    /// <summary>
    /// Applies <paramref name="mutate"/> to a private copy and commits that copy only if the
    /// delegate returns normally and <paramref name="shouldCommit"/> accepts its result.
    /// A throwing or rejected delegate leaves the store untouched, which is what makes pool
    /// creation all-or-nothing (spec 001 NFR-005, EC-8).
    /// </summary>
    /// <param name="shouldCommit">
    /// Decides whether the half-finished work is kept. Omit it to always commit.
    /// </param>
    TResult Mutate<TResult>(Func<EkPronoData, TResult> mutate, Func<TResult, bool>? shouldCommit = null);
}
