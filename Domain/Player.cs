namespace EKProno.Domain;

/// <summary>
/// A participant in a pool, identified by their display name within that pool
/// (spec 001 §5.1).
/// </summary>
public sealed class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoolId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsOrganiser { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public const int DisplayNameMaxLength = 50;
}
