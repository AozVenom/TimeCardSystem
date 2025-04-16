using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TimeCardSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace TimeCardSystem.API.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public void OnGet()
        {
            _logger.LogInformation("Login page accessed");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation($"Login attempt for {Input.Email}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                return Page();
            }

            try
            {
                // First find the user
                var user = await _userManager.FindByEmailAsync(Input.Email);

                if (user == null)
                {
                    _logger.LogWarning($"No user found for email {Input.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                    return Page();
                }

                // Check the password
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, Input.Password);

                if (!isPasswordValid)
                {
                    _logger.LogWarning($"Invalid password for {Input.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                    return Page();
                }

                // Create claims for the user
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName ?? user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    // Add role claim explicitly
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };

                // Log the role for debugging
                _logger.LogInformation($"User role: {user.Role}");

                // Create claims identity and sign in
                var claimsIdentity = new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme);

                await HttpContext.SignInAsync(
                    IdentityConstants.ApplicationScheme,  // Use Identity's application scheme
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) // Cookie expiration
                    }
                );

                _logger.LogInformation($"Successful login for {Input.Email}");
                _logger.LogInformation($"Redirecting to Dashboard. User ID: {user.Id}");

                return RedirectToPage("/Dashboard/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during login for {Input.Email}");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            _logger.LogInformation("User logging out");
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);  // Use Identity's application scheme
            return RedirectToPage("/Account/Login");
        }
    }
}