using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

public class IndexModel(PoolService pools) : PageModel
{
    public IReadOnlyList<PoolSummary> Pools { get; private set; } = [];

    public IActionResult OnGet()
    {
        if (User.UserId() is not { } organiserId)
        {
            return Challenge();
        }

        // FR-011 / SC-008: only the pools this organiser owns, newest first.
        Pools = pools.ListPoolsOwnedBy(organiserId);
        return Page();
    }
}
