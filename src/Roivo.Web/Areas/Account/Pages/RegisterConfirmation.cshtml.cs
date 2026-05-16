using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Roivo.Web.Areas.Account.Pages;

public class RegisterConfirmationModel : PageModel
{
    public string? Email { get; set; }

    public void OnGet(string? email)
    {
        Email = email;
    }
}
