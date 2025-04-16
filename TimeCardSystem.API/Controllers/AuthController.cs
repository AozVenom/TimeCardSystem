using Microsoft.AspNetCore.Mvc;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Dtos;

namespace TimeCardSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(UserRegistrationDto registrationDto)
        {
            try
            {
                var response = await _authService.RegisterAsync(registrationDto);
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                var response = await _authService.LoginAsync(loginDto);
                return Ok(response);
            }
            catch (ApplicationException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        public async Task<ActionResult> ChangePassword(string userId, string currentPassword, string newPassword)
        {
            try
            {
                var result = await _authService.ChangePasswordAsync(userId, currentPassword, newPassword);
                return result ? Ok() : BadRequest("Password change failed");
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}