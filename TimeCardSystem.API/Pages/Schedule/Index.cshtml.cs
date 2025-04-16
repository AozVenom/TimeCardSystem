using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models; // Ensure this is correct
using TimeCardSystem.API.ViewModels; // Use your view models

namespace TimeCardSystem.API.Pages.Schedule
{
    [Authorize(Roles = "Manager,Administrator")]
    public class IndexModel : PageModel
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;

        [BindProperty(SupportsGet = true)]
        public ScheduleSearchViewModel SearchViewModel { get; set; } = new ScheduleSearchViewModel();

        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();

        public IEnumerable<ScheduleItemViewModel> Schedules { get; set; } = new List<ScheduleItemViewModel>();

        public IndexModel(
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository)
        {
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadEmployeesAsync();
            await LoadSchedulesAsync();
            return Page();
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

        private async Task LoadSchedulesAsync()
        {
            Schedules = await _scheduleRepository.SearchSchedulesAsync(
                SearchViewModel.EmployeeId,
                SearchViewModel.StartDate,
                SearchViewModel.EndDate,
                SearchViewModel.Status
            );
        }
    }
}