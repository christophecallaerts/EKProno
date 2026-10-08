using System.ComponentModel.DataAnnotations;
using EKProno.Domain;
using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

public class CreateModel(PoolService pools) : PageModel
{
    // Nullable on purpose: it keeps MVC's implicit [Required] off these fields so
    // PoolService stays the single source of validation, wording included.
    [BindProperty]
    [Display(Name = "Pool name")]
    public string? PoolName { get; set; }

    /// <summary>FR-004: pick an existing tournament, or describe a new one.</summary>
    [BindProperty]
    public bool UseExistingTournament { get; set; }

    [BindProperty]
    [Display(Name = "Tournament")]
    public Guid? TournamentId { get; set; }

    [BindProperty]
    [Display(Name = "Tournament name")]
    public string? TournamentName { get; set; }

    [BindProperty]
    [Display(Name = "Edition")]
    public string? TournamentEdition { get; set; }

    public IReadOnlyList<Tournament> Tournaments { get; private set; } = [];

    public IActionResult OnGet()
    {
        if (User.UserId() is not { } organiserId)
        {
            return Challenge();
        }

        LoadTournaments(organiserId);
        UseExistingTournament = Tournaments.Count > 0;
        return Page();
    }

    public IActionResult OnPost()
    {
        // FR-010 / SC-007 / EC-7: posting here without an account creates nothing.
        if (User.UserId() is not { } organiserId)
        {
            return Challenge();
        }

        LoadTournaments(organiserId);

        TournamentChoice choice = UseExistingTournament
            ? new TournamentChoice.Existing(TournamentId ?? Guid.Empty)
            : new TournamentChoice.New(TournamentName ?? string.Empty, TournamentEdition ?? string.Empty);

        var result = pools.CreatePool(organiserId, new CreatePoolRequest(PoolName ?? string.Empty, choice));

        if (result.Failed)
        {
            ModelState.AddModelError(result.Error.Field, result.Error.Message);
            return Page();
        }

        // FR-015: straight on to entering the schedule (spec 002).
        return RedirectToPage("Schedule", new { id = result.Value.Id });
    }

    private void LoadTournaments(Guid organiserId) =>
        Tournaments = pools.ListTournamentsOwnedBy(organiserId);
}
