using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;

namespace WebApp.Pages.Colleagues;

public class EditModel(ApplicationDbContext context, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    [BindProperty]
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var colleague = await context.Colleagues.FirstOrDefaultAsync(c => c.Id == Id && c.UserId == userId);
        if (colleague is null)
        {
            return NotFound();
        }

        Name = colleague.Name;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var colleague = await context.Colleagues.FirstOrDefaultAsync(c => c.Id == Id && c.UserId == userId);
        if (colleague is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        colleague.Name = Name.Trim();
        await context.SaveChangesAsync();

        return RedirectToPage("/Colleagues/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = userManager.GetUserId(User)!;

        var colleague = await context.Colleagues.FirstOrDefaultAsync(c => c.Id == Id && c.UserId == userId);
        if (colleague is null)
        {
            return NotFound();
        }

        context.Colleagues.Remove(colleague);
        await context.SaveChangesAsync();

        return RedirectToPage("/Colleagues/Index");
    }
}
