using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.Admin
{
    [Authorize(Roles = "Administrator,3")]
    public class UsersModel : PageModel
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UsersModel> _logger;

        public UsersModel(
            IUserRepository userRepository,
            ILogger<UsersModel> logger)
        {
            _userRepository = userRepository;
            _logger = logger;

            // Initialize non-nullable property
            StatusMessage = string.Empty;
        }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        public List<User> Users { get; set; } = new List<User>();

        public string StatusMessage { get; set; }

        public bool IsError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {

                var allUsers = await _userRepository.GetAllAsync();

                // Calculate pagination
                int totalUsers = allUsers.Count();
                TotalPages = (int)Math.Ceiling(totalUsers / (double)PageSize);

                // Apply pagination
                Users = allUsers
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                // Retrieve status message from TempData if available
                if (TempData["StatusMessage"] != null)
                {
                    // Fix for null reference warning - check if TempData value exists
                    var statusMsg = TempData["StatusMessage"]?.ToString();
                    if (statusMsg != null)
                    {
                        StatusMessage = statusMsg;
                    }

                    // Fix for unboxing possibly null value - check if TempData value exists
                    if (TempData["IsError"] != null)
                    {
                        // Fix for line 78 - Safe unboxing of possibly null value
                        IsError = TempData["IsError"] is bool isError && isError;
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading users admin page");
                StatusMessage = "An error occurred while loading users.";
                IsError = true;
                return Page();
            }
        }

        public async Task<IActionResult> OnGetExportUsersAsync()
        {
            try
            {
                // Get all users
                var users = await _userRepository.GetAllAsync();

                // Create CSV content
                var csvBuilder = new StringBuilder();
                csvBuilder.AppendLine("ID,First Name,Last Name,Email,Role,Status,Created Date");

                foreach (var user in users)
                {
                    csvBuilder.AppendLine($"{user.Id},{user.FirstName},{user.LastName},{user.Email},{user.Role},{(user.IsActive ? "Active" : "Inactive")},{user.CreatedAt:yyyy-MM-dd}");
                }

                byte[] bytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

                return File(bytes, "text/csv", $"users_export_{DateTime.Now:yyyy-MM-dd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                TempData["StatusMessage"] = "An error occurred while exporting users.";
                TempData["IsError"] = true;
                return RedirectToPage();
            }
        }
    }
}