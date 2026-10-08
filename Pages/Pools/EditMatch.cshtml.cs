using System.ComponentModel.DataAnnotations;
using EKProno.Domain;
using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

/// <summary>US-004: correct a fixture that was entered wrongly (FR-013, FR-014).</summary>
public class EditMatchModel(ScheduleService schedule) : PageModel
{
    public ScheduleContext Context { get; private set; } = default!;
    public IReadOnlyList<TeamView> Teams { get; private set; } = [];

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
    public Stage Stage { get; set; }

    [TempData]
    public string? Notice { get; set; }

    [TempData]
    public string? Warning { get; set; }

    public IActionResult OnGet(Guid id, Guid matchId)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        if (Load(ownerId, id) is { } failure)
        {
            return failure;
        }

        var match = schedule.GetMatch(ownerId, id, matchId);
        if (match is null)
        {
            return NotFound();
        }

        HomeTeamId = match.HomeTeamId;
        AwayTeamId = match.AwayTeamId;
        Stage = match.Stage;
        LocalKickoff = TimeZoneInfo.ConvertTime(match.KickoffAt, ViewerTimeZone.Of(HttpContext)).DateTime;

        return Page();
    }

    public IActionResult OnPost(Guid id, Guid matchId)
    {
        if (User.UserId() is not { } ownerId)
        {
            return Challenge();
        }

        var timeZone = ViewerTimeZone.Of(HttpContext);
        var input = new MatchInput(HomeTeamId, AwayTeamId, LocalKickoff, timeZone.Id, Stage);
        var result = schedule.EditMatch(ownerId, id, matchId, input);

        if (result.Failed)
        {
            if (Load(ownerId, id) is { } failure)
            {
                return failure;
            }

            // EC-5, EC-6, EC-10: the stored match is untouched, so the form can simply
            // redisplay what was submitted alongside the reason it was refused.
            ModelState.AddModelError(result.Error.Field, result.Error.Message);
            return Page();
        }

        Notice = "Match updated.";
        Warning = result.Value.Warning;
        return RedirectToPage("Schedule", new { id });
    }

    private IActionResult? Load(Guid ownerId, Guid id)
    {
        if (schedule.GetContext(ownerId, id) is not { } context)
        {
            return NotFound();
        }

        Context = context;
        Teams = schedule.ListTeams(ownerId, id) ?? [];
        return null;
    }
}
