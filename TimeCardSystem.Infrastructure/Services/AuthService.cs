using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TimeCardSystem.Core.Dtos;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<AuthResponseDto> RegisterAsync(UserRegistrationDto registrationDto)
        {
            var user = new User
            {
                UserName = registrationDto.Email,
                Email = registrationDto.Email,
                FirstName = registrationDto.FirstName,
                LastName = registrationDto.LastName,
                Role = registrationDto.Role
            };

            var result = await _userManager.CreateAsync(user, registrationDto.Password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                string errorMessage = string.Join(", ", errors);

                Console.WriteLine($"User registration failed: {errorMessage}");

                throw new ApplicationException($"User registration failed: {errorMessage}");
            }

            Console.WriteLine($"User registered successfully: {user.Email}, ID: {user.Id}");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                // Token generation removed for Cookie Authentication
                Expiration = DateTime.UtcNow.AddHours(3)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                Console.WriteLine($"Login failed: User not found for email {loginDto.Email}");
                throw new ApplicationException("Invalid login attempt");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                Console.WriteLine($"Login failed: Invalid password for user {loginDto.Email}");
                throw new ApplicationException("Invalid login attempt");
            }

            Console.WriteLine($"Login successful for user: {user.Email}, ID: {user.Id}");

            return new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                Role = user.Role,
                // Token generation removed for Cookie Authentication
                Expiration = DateTime.UtcNow.AddHours(3)
            };
        }

        public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                Console.WriteLine($"Change password failed: User not found for ID {userId}");
                throw new ApplicationException("User not found");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                Console.WriteLine($"Password change failed: {string.Join(", ", errors)}");
            }

            return result.Succeeded;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }
    }
}