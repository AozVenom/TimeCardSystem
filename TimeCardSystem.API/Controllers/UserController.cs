using Microsoft.AspNetCore.Mvc;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Core.Interfaces;

namespace TimeCardSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _userRepository.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<User>> GetUserByEmail(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
            if (id != updatedUser.Id)
            {
                return BadRequest();
            }

            try
            {
                await _userRepository.UpdateAsync(updatedUser);
                return NoContent();
            }
            catch
            {
                if (!await UserExists(id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await _userRepository.DeleteAsync(id);
            return NoContent();
        }

        private async Task<bool> UserExists(string id)
        {
            return await _userRepository.ExistsAsync(id);
        }
    }
}