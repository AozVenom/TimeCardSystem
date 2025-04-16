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

        [BindProperty]
        public ScheduleCreateViewModel ScheduleViewModel { get; set; }

        public List<SelectListItem> Employees { get; set; }

        public CreateModel(
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository)
        {
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadEmployeesAsync();
            ScheduleViewModel = new ScheduleCreateViewModel();
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
                var createdById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var schedule = ScheduleViewModel.ToScheduleModel(createdById);

                await _scheduleRepository.AddAsync(schedule);

                TempData["SuccessMessage"] = "Schedule created successfully.";
                return RedirectToPage("/Schedule/Index");
            }
            catch (Exception ex)
            {
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