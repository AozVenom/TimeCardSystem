using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TimeCardSystem.API.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<TimeCardSystem.Core.Models.User> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(
            SignInManager<TimeCardSystem.Core.Models.User> signInManager,
            ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // When called with GET, perform the logout and redirect
            return await PerformLogout();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // When called with POST, perform the logout and redirect
            return await PerformLogout();
        }

        private async Task<IActionResult> PerformLogout()
        {
            _logger.LogInformation("User logged out");

            // Sign out of the Identity system
            await _signInManager.SignOutAsync();

            // Clear all authentication cookies
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // Redirect to login page
            return RedirectToPage("/Account/Login");
        }
    }
}