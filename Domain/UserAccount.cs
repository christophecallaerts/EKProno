namespace EKProno.Domain;

/// <summary>
/// The login identity behind an organiser or a player (spec 001 §5.1).
/// </summary>
public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public const int EmailMaxLength = 255;
    public const int DisplayNameMaxLength = 50;

    /// <summary>
    /// EC-9: the local part of the email, used when the account has no usable display name.
    /// </summary>
    public string DisplayNameOrEmailLocalPart()
    {
        if (!string.IsNullOrWhiteSpace(DisplayName))
        {
            return DisplayName.Trim();
        }

        var at = Email.IndexOf('@');
        var local = at > 0 ? Email[..at] : Email;
        return string.IsNullOrWhiteSpace(local) ? "Player" : local.Trim();
    }
}
