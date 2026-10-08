using System.ComponentModel.DataAnnotations;
using EKProno.Domain;
using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

/// <summary>
/// The schedule editor of spec 002: the fixture list (US-005) with the add-a-match form
/// (US-002, US-003) above it.
/// </summary>
public class ScheduleModel(ScheduleService schedule) : PageModel
{
    public ScheduleView Schedule { get; private set; } = default!;

    [BindProperty]
    [Display(Name = "Home team")]
    public Guid HomeTeamId { get; set; }

    [BindProperty]
    [Display(Name = "Away team")]
    public Guid AwayTeamId { get; set; }

    [BindProperty]
    [Display(Name = "Kickoff")]
    [DataType(DataType.DateTime)]
    public DateTime? LocalKickoff { get; set; }

    [BindProperty]
    public Stage Stage { get; set; } = Stage.GroupStage;

    [TempData]
    public string? Notice { get; set; }

    [TempData]
    public string? Warning { get; set; }

    /// <summary>NFR-004: the previous entry's stage and date come back pre-filled.</summary>
    [TempData]
    public string? LastStage { get; set; }

    /// <remarks>
    /// A <see cref="DateTime"/> rather than a string on purpose: TempData's serialiser
    /// reads any date-shaped string back as a <see cref="DateTime"/>, so a string property
    /// here throws on the next GET.
    /// </remarks>
    [TempData]
    public DateTime? LastKickoff { get; set; }

    public IActionResult OnGet(Guid id)
    {
        if (Load(id) is { } redirect)
        {
            return redirect;
        }

        if (LastStage is { Length: > 0 } && Enum.TryParse<Stage>(LastStage, out var stage))
        {
            Stage = stage;
        }

        LocalKickoff ??= LastKickoff;

        return Page();
    }

    public IActionResult OnPostAdd(Guid id)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        var timeZone = ViewerTimeZone.Of(HttpContext);
        var input = new MatchInput(HomeTeamId, AwayTeamId, LocalKickoff, timeZone.Id, Stage);
        var result = schedule.AddMatch(ownerId, id, input);

        if (result.Failed)
        {
            if (schedule.GetContext(ownerId, id) is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(result.Error.Field, result.Error.Message);
            return Load(id) ?? Page();
        }

        Notice = "Match added.";
        Warning = result.Value.Warning;

        // Keep the stage, and the date without its time: the next fixture is usually the
        // same stage on the same day (NFR-004, SC-002).
        LastStage = Stage.ToString();
        LastKickoff = LocalKickoff?.Date;

        return RedirectToPage(new { id });
    }

    public IActionResult OnPostDelete(Guid id, Guid matchId)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        var result = schedule.DeleteMatch(ownerId, id, matchId);

        if (result.Failed)
        {
            if (schedule.GetContext(ownerId, id) is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(result.Error.Field, result.Error.Message);
            return Load(id) ?? Page();
        }

        Notice = "Match deleted.";
        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Loads the schedule, or returns the result to send instead. <c>null</c> means the page
    /// is ready to render.
    /// </summary>
    private IActionResult? Load(Guid id)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        // NFR-003 / SC-013: somebody else's tournament simply is not there.
        if (schedule.GetSchedule(ownerId, id, ViewerTimeZone.Of(HttpContext)) is not { } view)
        {
            return NotFound();
        }

        Schedule = view;
        return null;
    }
}
