using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.API.ViewModels;
using Microsoft.Extensions.Logging;

namespace TimeCardSystem.API.Pages.Schedule
{
    [Authorize(Roles = "Manager,Administrator")]
    public class EditModel : PageModel
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<EditModel> _logger;

        [BindProperty]
        public ScheduleCreateViewModel ScheduleUpdate { get; set; } = new ScheduleCreateViewModel();

        [BindProperty]
        public int ScheduleId { get; set; }

        public string EmployeeName { get; set; } = string.Empty;
        public DateTime OriginalShiftStart { get; set; }

        public EditModel(
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository,
            ILogger<EditModel> logger)
        {
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id);
            if (schedule == null)
            {
                return RedirectToPage("/Schedule/Index");
            }

            // Get employee name
            var user = await _userRepository.GetByIdAsync(schedule.UserId);
            EmployeeName = $"{user?.FirstName} {user?.LastName}".Trim();

            // Determine lunch option based on BreakDuration
            string lunchOption = "None";
            if (schedule.BreakDuration.HasValue)
            {
                int minutes = (int)schedule.BreakDuration.Value.TotalMinutes;
                lunchOption = minutes >= 60 ? "60" : "30";
            }

            // Populate the view model
            ScheduleUpdate = new ScheduleCreateViewModel
            {
                EmployeeId = schedule.UserId,
                ShiftStart = schedule.ShiftStart,
                ShiftEnd = schedule.ShiftEnd,
                Location = schedule.Location,
                Notes = schedule.Notes,
                LunchOption = lunchOption,
                Status = schedule.Status
            };

            ScheduleId = schedule.Id;
            OriginalShiftStart = schedule.ShiftStart;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Get the existing schedule
                var existingSchedule = await _scheduleRepository.GetByIdAsync(ScheduleId);
                if (existingSchedule == null)
                {
                    return RedirectToPage("/Schedule/Index");
                }

                // Update the schedule
                existingSchedule.ShiftStart = ScheduleUpdate.ShiftStart;
                existingSchedule.ShiftEnd = ScheduleUpdate.ShiftEnd;
                existingSchedule.Location = ScheduleUpdate.Location;
                existingSchedule.Notes = ScheduleUpdate.Notes;
                existingSchedule.Status = ScheduleUpdate.Status;

                // Set break duration and HasLunch based on LunchOption
                existingSchedule.BreakDuration = ScheduleUpdate.BreakDuration;
                existingSchedule.HasLunch = ScheduleUpdate.LunchOption != "None";

                if (existingSchedule.HasLunch && existingSchedule.BreakDuration.HasValue)
                {
                    existingSchedule.LunchDuration = existingSchedule.BreakDuration.Value;
                }

                existingSchedule.ModifiedAt = DateTime.UtcNow;

                await _scheduleRepository.UpdateAsync(existingSchedule);

                TempData["SuccessMessage"] = "Schedule updated successfully.";
                return RedirectToPage("/Schedule/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating schedule {id}", ScheduleId);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the schedule.");
                return Page();
            }
        }
    }
}