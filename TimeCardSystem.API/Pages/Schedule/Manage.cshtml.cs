using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimeCardSystem.API.ViewModels;
using TimeCardSystem.Core.Dtos;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.Schedule
{
    [Authorize(Roles = "Manager,Administrator,2,3")]
    public class ManageModel : PageModel
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ManageModel> _logger;

        public ManageModel(
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository,
            ILogger<ManageModel> logger)
        {
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        [BindProperty]
        public CreateScheduleDto NewSchedule { get; set; } = new CreateScheduleDto();

        public List<SelectListItem> TeamMembers { get; set; } = new List<SelectListItem>();

        public List<EmployeeScheduleViewModel> WeeklySchedules { get; set; } = new List<EmployeeScheduleViewModel>();

        public DateTime CurrentWeekStart { get; set; }

        public DateTime CurrentWeekEnd { get; set; }

        public async Task<IActionResult> OnGetAsync(DateTime? weekStart = null)
        {
            try
            {
                // Setup week dates
                if (weekStart.HasValue && weekStart.Value.Year > 2000)
                {
                    CurrentWeekStart = weekStart.Value.Date;
                }
                else
                {
                    // Default to the current week (starting on Monday)
                    var today = DateTime.Today;
                    var dayOfWeek = (int)today.DayOfWeek;
                    CurrentWeekStart = today.AddDays(dayOfWeek == 0 ? -6 : 1 - dayOfWeek).Date;
                }

                CurrentWeekEnd = CurrentWeekStart.AddDays(6);

                // Set default shift start date to the Monday of the current week view, at 9:00 AM
                var defaultShiftStart = CurrentWeekStart.AddHours(9);
                if (NewSchedule.ShiftStart == default(DateTime) ||
                    (NewSchedule.ShiftStart.Year == 1 && NewSchedule.ShiftStart.Month == 1))
                {
                    NewSchedule.ShiftStart = defaultShiftStart;

                    // If you want to also set a default end time (8 hours + 30 min lunch)
                    NewSchedule.ShiftEnd = defaultShiftStart.AddHours(8).AddMinutes(30);
                }

                // Get team members for the dropdown
                await LoadTeamMembersAsync();

                // Get weekly schedules
                await LoadWeeklySchedulesAsync();

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedule management page: {Message}", ex.Message);
                return RedirectToPage("/Error");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadTeamMembersAsync();
                await LoadWeeklySchedulesAsync();
                return Page();
            }

            try
            {
                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Check for conflicts
                var hasConflict = await _scheduleRepository.HasOverlappingScheduleAsync(
                    NewSchedule.UserId,
                    NewSchedule.ShiftStart,
                    NewSchedule.ShiftEnd);

                if (hasConflict)
                {
                    ModelState.AddModelError(string.Empty, "This schedule conflicts with an existing schedule for the selected employee.");
                    await LoadTeamMembersAsync();
                    await LoadWeeklySchedulesAsync();
                    return Page();
                }

                // Determine if we have a lunch break based on the LunchOption
                bool hasLunch = NewSchedule.LunchOption != "None";
                TimeSpan? breakDuration = null;
                TimeSpan lunchDuration = TimeSpan.Zero;

                if (hasLunch)
                {
                    if (NewSchedule.LunchOption == "30")
                    {
                        breakDuration = TimeSpan.FromMinutes(30);
                        lunchDuration = TimeSpan.FromMinutes(30);
                    }
                    else if (NewSchedule.LunchOption == "60")
                    {
                        breakDuration = TimeSpan.FromHours(1);
                        lunchDuration = TimeSpan.FromHours(1);
                    }
                }

                var schedule = new Core.Models.Schedule
                {
                    UserId = NewSchedule.UserId,
                    ShiftStart = NewSchedule.ShiftStart,
                    ShiftEnd = NewSchedule.ShiftEnd,
                    Location = NewSchedule.Location,
                    Notes = NewSchedule.Notes,
                    BreakDuration = breakDuration,
                    Status = ScheduleStatus.Pending,
                    CreatedById = managerId,
                    CreatedAt = DateTime.UtcNow,
                    HasLunch = hasLunch,
                    LunchDuration = lunchDuration
                };

                await _scheduleRepository.AddAsync(schedule);

                // Redirect to the same page for the week of the new schedule
                return RedirectToPage("/Schedule/Manage", new { weekStart = NewSchedule.ShiftStart.Date });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the schedule.");
                await LoadTeamMembersAsync();
                await LoadWeeklySchedulesAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, string weekStart = null)
        {
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)
                {
                    return NotFound();
                }

                await _scheduleRepository.DeleteAsync(id);

                // If weekStart was provided, use it; otherwise use the current week
                DateTime redirectDate = !string.IsNullOrEmpty(weekStart) ?
                    DateTime.Parse(weekStart) : CurrentWeekStart;

                // Redirect back to the same week view
                return RedirectToPage("/Schedule/Manage", new { weekStart = redirectDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting schedule {id}");
                return RedirectToPage("/Error");
            }
        }

        public async Task<IActionResult> OnPostPublishAsync(int id, string weekStart = null)
        {
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)
                {
                    return NotFound();
                }

                schedule.Status = ScheduleStatus.Published;
                schedule.ModifiedAt = DateTime.UtcNow;

                await _scheduleRepository.UpdateAsync(schedule);

                // If weekStart was provided, use it; otherwise use the current week
                DateTime redirectDate = !string.IsNullOrEmpty(weekStart) ?
                    DateTime.Parse(weekStart) : CurrentWeekStart;

                // Redirect back to the same week view
                return RedirectToPage("/Schedule/Manage", new { weekStart = redirectDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing schedule {id}");
                return RedirectToPage("/Error");
            }
        }

        private async Task LoadTeamMembersAsync()
        {
            // This would normally filter to only show team members for the current manager
            // For simplicity, we'll get all employees
            var users = await _userRepository.GetAllAsync();
            var employees = users.Where(u => u.Role == UserRole.Employee || u.Role.ToString() == "1");

            TeamMembers = employees.Select(e => new SelectListItem
            {
                Value = e.Id,
                Text = $"{e.FirstName} {e.LastName} ({e.Email})"
            }).ToList();
        }

        private async Task LoadWeeklySchedulesAsync()
        {
            // For a real implementation, you would filter schedules for the current manager's team
            var users = await _userRepository.GetAllAsync();
            var employees = users.Where(u => u.Role == UserRole.Employee || u.Role.ToString() == "1").ToList();

            WeeklySchedules = new List<EmployeeScheduleViewModel>();

            foreach (var employee in employees)
            {
                var schedules = await _scheduleRepository.GetByUserIdAndDateRangeAsync(
                    employee.Id,
                    CurrentWeekStart,
                    CurrentWeekEnd.AddDays(1).AddSeconds(-1));

                var employeeViewModel = new EmployeeScheduleViewModel
                {
                    EmployeeId = employee.Id,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    Email = employee.Email,
                    DailySchedules = new List<DailyScheduleViewModel>()
                };

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = CurrentWeekStart.AddDays(i).Date;
                    var schedulesForDay = schedules.Where(s =>
                        (s.ShiftStart.Date <= currentDate && s.ShiftEnd.Date >= currentDate) ||
                        s.ShiftStart.Date == currentDate ||
                        s.ShiftEnd.Date == currentDate
                    ).ToList();

                    var dailySchedule = new DailyScheduleViewModel
                    {
                        Date = currentDate,
                        DayOfWeek = currentDate.DayOfWeek.ToString(),
                        Schedules = schedulesForDay.Select(s => new ScheduleItemViewModel
                        {
                            Id = s.Id,
                            ShiftStart = s.ShiftStart,
                            ShiftEnd = s.ShiftEnd,
                            StartTime = s.ShiftStart.ToString("HH:mm"),
                            EndTime = s.ShiftEnd.ToString("HH:mm"),
                            Location = s.Location,
                            Notes = s.Notes,
                            Status = s.Status,
                            StatusName = s.Status.ToString(),
                            BreakDuration = s.BreakDuration,
                            TotalHours = Math.Round((s.ShiftEnd - s.ShiftStart).TotalHours - (s.BreakDuration?.TotalHours ?? 0), 1)
                        }).ToList()
                    };

                    employeeViewModel.DailySchedules.Add(dailySchedule);
                }

                WeeklySchedules.Add(employeeViewModel);
            }
        }
    }
}