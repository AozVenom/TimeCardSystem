using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TimeCardSystem.API.ViewModels;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.Schedule
{
    [Authorize(Roles = "Manager,Administrator")]
    public class CreateModel : PageModel
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateModel> _logger;

        [BindProperty]
        public ScheduleCreateViewModel ScheduleViewModel { get; set; } = new ScheduleCreateViewModel();

        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();

        public CreateModel(
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository,
            ILogger<CreateModel> logger)
        {
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadEmployeesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadEmployeesAsync();
                return Page();
            }

            try
            {
                var createdById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var schedule = ScheduleViewModel.ToScheduleModel(createdById);

                await _scheduleRepository.AddAsync(schedule);

                TempData["SuccessMessage"] = "Schedule created successfully.";
                return RedirectToPage("/Schedule/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the schedule.");
                await LoadEmployeesAsync();
                return Page();
            }
        }

        private async Task LoadEmployeesAsync()
        {
            var employees = await _userRepository.GetEmployeesAsync();
            Employees = employees.Select(e => new SelectListItem
            {
                Value = e.Id,
                Text = $"{e.FirstName} {e.LastName}"
            }).ToList();
        }
    }
}