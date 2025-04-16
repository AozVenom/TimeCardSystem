using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Pages.TimeEntries
{
    public class IndexModel : PageModel
    {
        private readonly ITimeEntryRepository _timeEntryRepository;

        public IEnumerable<TimeEntry> TimeEntries { get; set; }

        public IndexModel(ITimeEntryRepository timeEntryRepository)
        {
            _timeEntryRepository = timeEntryRepository;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            TimeEntries = await _timeEntryRepository.GetByUserIdAsync(userId);
            return Page();
        }
    }
}