using System.Threading.Tasks;
using TimeCardSystem.Core.Dtos;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(UserRegistrationDto registrationDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
        Task<User> GetUserByIdAsync(string userId);
    }
}