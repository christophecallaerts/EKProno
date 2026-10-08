using System.Text.Json;

namespace EKProno.Storage;

/// <summary>
/// Keeps the whole data set in memory and mirrors it to a JSON file so it survives a
/// restart. Registered as a singleton; all access is serialised by a single lock.
/// </summary>
public sealed class JsonFileDataStore : IDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly Lock _gate = new();
    private readonly string? _filePath;
    private EkPronoData _data;

    /// <param name="filePath">
    /// Where to mirror the data. Pass <c>null</c> to stay purely in memory, which is what
    /// the tests do.
    /// </param>
    public JsonFileDataStore(string? filePath)
    {
        _filePath = filePath;
        _data = Load(filePath) ?? new EkPronoData();
    }

    public TResult Query<TResult>(Func<EkPronoData, TResult> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (_gate)
        {
            // Hand out a copy so callers cannot mutate committed state by accident.
            return query(Clone(_data));
        }
    }

    public TResult Mutate<TResult>(Func<EkPronoData, TResult> mutate, Func<TResult, bool>? shouldCommit = null)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        lock (_gate)
        {
            var working = Clone(_data);
            var result = mutate(working);

            if (shouldCommit is not null && !shouldCommit(result))
            {
                // Discard `working` entirely — partial writes never reach the store.
                return result;
            }

            Persist(working);
            _data = working;

            return result;
        }
    }

    private void Persist(EkPronoData data)
    {
        if (_filePath is null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file first so a crash mid-write cannot truncate the store.
        var temporaryPath = _filePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, SerializerOptions));
        File.Move(temporaryPath, _filePath, overwrite: true);
    }

    private static EkPronoData? Load(string? filePath)
    {
        if (filePath is null || !File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<EkPronoData>(json, SerializerOptions);
    }

    private static EkPronoData Clone(EkPronoData data) =>
        JsonSerializer.Deserialize<EkPronoData>(
            JsonSerializer.Serialize(data, SerializerOptions), SerializerOptions)!;
}
