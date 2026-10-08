using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

/// <summary>
/// Placeholder landing page for FR-015. Spec 002 turns this into the schedule editor.
/// </summary>
public class ScheduleModel(PoolService pools) : PageModel
{
    public string PoolName { get; private set; } = string.Empty;
    public string TournamentName { get; private set; } = string.Empty;

    public IActionResult OnGet(Guid id)
    {
        if (User.UserId() is not { } organiserId)
        {
            return Challenge();
        }

        var pool = pools.GetPoolOwnedBy(organiserId, id);
        if (pool is null)
        {
            return NotFound();
        }

        PoolName = pool.Name;
        TournamentName = pools.ListPoolsOwnedBy(organiserId)
            .SingleOrDefault(p => p.Id == id)?.TournamentName ?? string.Empty;

        return Page();
    }
}
