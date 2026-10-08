using System.ComponentModel.DataAnnotations;
using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

/// <summary>US-001: the tournament's teams (FR-001..FR-005).</summary>
public class TeamsModel(ScheduleService schedule) : PageModel
{
    public ScheduleContext Context { get; private set; } = default!;
    public IReadOnlyList<TeamView> Teams { get; private set; } = [];

    // Nullable on purpose — see CreateModel.PoolName.
    [BindProperty]
    [Display(Name = "Team name")]
    public string? Name { get; set; }

    [TempData]
    public string? Notice { get; set; }

    public IActionResult OnGet(Guid id) => Load(id);

    public IActionResult OnPostAdd(Guid id)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        var result = schedule.AddTeam(ownerId, id, Name);

        if (result.Failed)
        {
            return Fail(id, result.Error);
        }

        Notice = $"Added {result.Value.Name}.";
        // Straight back to an empty field: organisers add teams in a burst (NFR-004).
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostRename(Guid id, Guid teamId)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        var result = schedule.RenameTeam(ownerId, id, teamId, Name);

        if (result.Failed)
        {
            return Fail(id, result.Error);
        }

        Notice = $"Renamed to {result.Value.Name}.";
        return RedirectToPage(new { id });
    }

    public IActionResult OnPostRemove(Guid id, Guid teamId)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        var result = schedule.RemoveTeam(ownerId, id, teamId);

        if (result.Failed)
        {
            return Fail(id, result.Error);
        }

        Notice = $"Removed {result.Value.Name}.";
        return RedirectToPage(new { id });
    }

    private IActionResult Fail(Guid id, Error error)
    {
        // SC-013: a pool that is not ours is reported as missing, never as a validation error.
        if (schedule.GetContext(User.UserId()!.Value, id) is null)
        {
            return NotFound();
        }

        ModelState.AddModelError(error.Field, error.Message);
        return Load(id);
    }

    private IActionResult Load(Guid id)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        if (schedule.GetContext(ownerId, id) is not { } context)
        {
            return NotFound();
        }

        Context = context;
        Teams = schedule.ListTeams(ownerId, id) ?? [];
        return Page();
    }
}
