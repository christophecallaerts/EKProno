using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EKProno.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Account;

public class SignInModel(UserAccountService accounts) : PageModel
{
    // Nullable on purpose — see Pools/CreateModel.PoolName.
    [BindProperty]
    [Display(Name = "Email address")]
    public string? Email { get; set; }

    [BindProperty]
    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        var result = accounts.SignIn(Email ?? string.Empty, DisplayName);

        if (result.Failed)
        {
            ModelState.AddModelError(result.Error.Field, result.Error.Message);
            return Page();
        }

        var account = result.Value;
        var identity = new ClaimsIdentity(
            [
                new Claim(UserAccountService.UserIdClaimType, account.Id.ToString()),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Name, account.DisplayNameOrEmailLocalPart()),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToPage("/Pools/Index");
    }
}
