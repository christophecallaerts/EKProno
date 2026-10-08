using System.Security.Claims;
using EKProno.Domain;
using EKProno.Storage;

namespace EKProno.Services;

/// <summary>
/// Stands in for the authentication feature spec 001 §9.1 assumes already exists: it only
/// resolves an email to a <see cref="UserAccount"/>, with no password and no verification.
/// Replace it wholesale when real registration and login land (spec 001 open question 1).
/// </summary>
public sealed class UserAccountService(IDataStore store, TimeProvider clock)
{
    public const string UserIdClaimType = "ekprono:user-id";

    public Result<UserAccount> SignIn(string email, string? displayName)
    {
        var normalisedEmail = (email ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(normalisedEmail) || !normalisedEmail.Contains('@'))
        {
            return Result<UserAccount>.Failure("Email", "Enter the email address you use for EKProno.");
        }

        if (normalisedEmail.Length > UserAccount.EmailMaxLength)
        {
            return Result<UserAccount>.Failure(
                "Email", $"An email address may be at most {UserAccount.EmailMaxLength} characters.");
        }

        var name = (displayName ?? string.Empty).Trim();
        if (name.Length > UserAccount.DisplayNameMaxLength)
        {
            return Result<UserAccount>.Failure(
                "DisplayName", $"A display name may be at most {UserAccount.DisplayNameMaxLength} characters.");
        }

        return store.Mutate(data =>
        {
            var account = data.UserAccounts.SingleOrDefault(
                u => string.Equals(u.Email, normalisedEmail, StringComparison.OrdinalIgnoreCase));

            if (account is null)
            {
                account = new UserAccount
                {
                    Email = normalisedEmail,
                    DisplayName = name,
                    CreatedAt = clock.GetUtcNow(),
                };
                data.UserAccounts.Add(account);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                account.DisplayName = name;
            }

            return Result<UserAccount>.Success(account);
        });
    }

    public UserAccount? Find(Guid id) =>
        store.Query(data => data.UserAccounts.SingleOrDefault(u => u.Id == id));
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>The signed-in account's id, or <c>null</c> for an anonymous visitor.</summary>
    public static Guid? UserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(UserAccountService.UserIdClaimType), out var id) ? id : null;
}
