using System.ComponentModel.DataAnnotations;
using EKProno.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EKProno.Pages.Pools;

public class RenameModel(PoolService pools) : PageModel
{
    // Nullable on purpose — see CreateModel.PoolName.
    [BindProperty]
    [Display(Name = "Pool name")]
    public string? Name { get; set; }

    public IActionResult OnGet(Guid id)
    {
        if (User.UserId() is not { } organiserId)
        {
            return Challenge();
        }

        // NFR-003 / SC-004: a pool that is not yours simply is not there.
        var pool = pools.GetPoolOwnedBy(organiserId, id);
        if (pool is null)
        {
            return NotFound();
        }

        Name = pool.Name;
        return Page();
    }

    public IActionResult OnPost(Guid id)
    {
        if (User.UserId() is not { } organiserId)
        {
            return Challenge();
        }

        var result = pools.RenamePool(organiserId, id, Name ?? string.Empty);

        if (result.Failed)
        {
            if (pools.GetPoolOwnedBy(organiserId, id) is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(result.Error.Field, result.Error.Message);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
