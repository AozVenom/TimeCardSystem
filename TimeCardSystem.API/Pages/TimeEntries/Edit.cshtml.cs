using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.TimeEntries
{
    public class EditModel : PageModel
    {
        private readonly ITimeEntryRepository _timeEntryRepository;

        [BindProperty]
        public TimeEntry TimeEntry { get; set; }

        public EditModel(ITimeEntryRepository timeEntryRepository)
        {
            _timeEntryRepository = timeEntryRepository;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            TimeEntry = await _timeEntryRepository.GetByIdAsync(id);

            if (TimeEntry == null || TimeEntry.UserId != userId)
            {
                return RedirectToPage("/TimeEntries/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ensure the user can only edit their own entries
            var existingEntry = await _timeEntryRepository.GetByIdAsync(TimeEntry.Id);
            if (existingEntry == null || existingEntry.UserId != userId)
            {
                return RedirectToPage("/TimeEntries/Index");
            }

            // Update only specific properties
            existingEntry.ClockIn = TimeEntry.ClockIn;
            existingEntry.ClockOut = TimeEntry.ClockOut;
            existingEntry.BreakDuration = TimeEntry.BreakDuration;
            existingEntry.Status = TimeEntry.Status;

            await _timeEntryRepository.UpdateAsync(existingEntry);
            return RedirectToPage("/TimeEntries/Index");
        }
    }
}