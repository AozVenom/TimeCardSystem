using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Dtos;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;

        public RegisterModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "First Name is required")]
            [StringLength(50, ErrorMessage = "First Name cannot be longer than 50 characters")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Last Name is required")]
            [StringLength(50, ErrorMessage = "Last Name cannot be longer than 50 characters")]
            public string LastName { get; set; }

            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email address")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var registerDto = new UserRegistrationDto
                {
                    Email = Input.Email,
                    Password = Input.Password,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Role = UserRole.Employee // Default role
                };

                await _authService.RegisterAsync(registerDto);

                // Redirect to login page after successful registration
                return RedirectToPage("/Account/Login");
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                return Page();
            }
        }
    }
}