using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,3")]
    public class UserAdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserAdminController> _logger;

        public UserAdminController(
            UserManager<User> userManager,
            IUserRepository userRepository,
            ILogger<UserAdminController> logger)
        {
            _userManager = userManager;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    role = (int)user.Role,
                    isActive = user.IsActive,
                    createdAt = user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving user {id}");
                return StatusCode(500, "An error occurred while retrieving the user.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userExists = await _userManager.FindByEmailAsync(model.Email);
                if (userExists != null)
                {
                    return BadRequest("A user with this email already exists.");
                }

                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = (UserRole)model.Role,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    EmailConfirmed = true // Auto-confirm for admin-created accounts
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Add user to appropriate role
                    await _userManager.AddToRoleAsync(user, user.Role.ToString());

                    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
                    {
                        id = user.Id,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        email = user.Email,
                        role = (int)user.Role,
                        isActive = user.IsActive,
                        createdAt = user.CreatedAt
                    });
                }

                return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "An error occurred while creating the user.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if current user is trying to modify themselves
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (id == currentUserId && (int)user.Role != model.Role)
                {
                    return BadRequest("You cannot change your own role.");
                }

                // Capture old role for role change logic
                var oldRole = user.Role;

                // Update basic info
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.NormalizedUserName = model.Email.ToUpper();
                user.Role = (UserRole)model.Role;

                // Password reset if requested
                if (model.ResetPassword && !string.IsNullOrEmpty(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                    if (!passwordResult.Succeeded)
                    {
                        return BadRequest(string.Join(", ", passwordResult.Errors.Select(e => e.Description)));
                    }
                }

                // Update user details
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
                }

                // Handle role change if needed
                if (oldRole != user.Role)
                {
                    // Remove from old role
                    await _userManager.RemoveFromRoleAsync(user, oldRole.ToString());

                    // Add to new role
                    await _userManager.AddToRoleAsync(user, user.Role.ToString());
                }

                return Ok(new
                {
                    id = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    role = (int)user.Role,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user {id}");
                return StatusCode(500, "An error occurred while updating the user.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if deleting self
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (id == currentUserId)
                {
                    return BadRequest("You cannot delete your own account.");
                }

                // Use the repository method to delete the user
                await _userRepository.DeleteAsync(id);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user {id}");
                return StatusCode(500, "An error occurred while deleting the user.");
            }
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleUserStatus(string id, [FromBody] ToggleStatusDto model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if toggling self
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (id == currentUserId && !model.IsActive)
                {
                    return BadRequest("You cannot deactivate your own account.");
                }

                user.IsActive = model.IsActive;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling status for user {id}");
                return StatusCode(500, "An error occurred while updating the user status.");
            }
        }

        [HttpPost("bulk-action")]
        public async Task<IActionResult> BulkAction([FromBody] BulkActionDto model)
        {
            try
            {
                if (model.UserIds == null || !model.UserIds.Any())
                {
                    return BadRequest("No users specified");
                }

                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                bool actionOnSelf = model.UserIds.Contains(currentUserId);
                var processedCount = 0;

                switch (model.Action)
                {
                    case "activate":
                        foreach (var userId in model.UserIds)
                        {
                            var user = await _userManager.FindByIdAsync(userId);
                            if (user != null)
                            {
                                user.IsActive = true;
                                await _userManager.UpdateAsync(user);
                                processedCount++;
                            }
                        }
                        break;

                    case "deactivate":
                        foreach (var userId in model.UserIds)
                        {
                            // Skip self
                            if (userId == currentUserId)
                                continue;

                            var user = await _userManager.FindByIdAsync(userId);
                            if (user != null)
                            {
                                user.IsActive = false;
                                await _userManager.UpdateAsync(user);
                                processedCount++;
                            }
                        }
                        break;

                    case "delete":
                        foreach (var userId in model.UserIds)
                        {
                            // Skip self
                            if (userId == currentUserId)
                                continue;

                            await _userRepository.DeleteAsync(userId);
                            processedCount++;
                        }
                        break;

                    case "changeRole":
                        if (!int.TryParse(model.Role, out int roleValue) || roleValue < 1 || roleValue > 3)
                        {
                            return BadRequest("Invalid role value");
                        }

                        var newRole = (UserRole)roleValue;

                        foreach (var userId in model.UserIds)
                        {
                            // Skip self
                            if (userId == currentUserId)
                                continue;

                            var user = await _userManager.FindByIdAsync(userId);
                            if (user != null)
                            {
                                var oldRole = user.Role;

                                if (oldRole != newRole)
                                {
                                    user.Role = newRole;
                                    await _userManager.UpdateAsync(user);

                                    await _userManager.RemoveFromRoleAsync(user, oldRole.ToString());
                                    await _userManager.AddToRoleAsync(user, newRole.ToString());

                                    processedCount++;
                                }
                            }
                        }
                        break;

                    default:
                        return BadRequest("Invalid action");
                }

                return Ok(new
                {
                    processedCount,
                    skippedSelf = actionOnSelf
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action");
                return StatusCode(500, "An error occurred while performing the bulk action.");
            }
        }
    }

    public class CreateUserDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(1, 3)]
        public int Role { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(1, 3)]
        public int Role { get; set; }

        public bool ResetPassword { get; set; }

        public string NewPassword { get; set; }
    }

    public class ToggleStatusDto
    {
        public bool IsActive { get; set; }
    }

    public class BulkActionDto
    {
        public List<string> UserIds { get; set; }
        public string Action { get; set; }
        public string Role { get; set; }
    }
}