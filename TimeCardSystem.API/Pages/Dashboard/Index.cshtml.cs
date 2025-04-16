using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Security.Claims;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Infrastructure.Repositories;

namespace TimeCardSystem.API.Pages.Dashboard
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITimeEntryRepository _timeEntryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IScheduleRepository _scheduleRepository;
        private readonly ILogger<IndexModel> _logger;

        public string UserName { get; set; } = string.Empty;
        public string UserID { get; set; } = string.Empty;
        public TimeEntry? ActiveTimeEntry { get; set; }
        public IEnumerable<TimeEntry> RecentTimeEntries { get; set; } = new List<TimeEntry>();
        public double WeeklyHours { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public IEnumerable<Core.Models.Schedule> UpcomingSchedules { get; set; } = new List<Core.Models.Schedule>();

        // Manager-specific properties
        public List<TeamMemberViewModel> TeamMembers { get; set; } = new List<TeamMemberViewModel>();
        public int TeamMemberCount { get; set; }
        public int ClockedInCount { get; set; }
        public int PendingApprovals { get; set; }

        // Admin-specific properties
        public int TotalUsers { get; set; }
        public int ActiveSessions { get; set; }
        public double TotalHoursToday { get; set; }
        public List<SystemActivityViewModel> RecentSystemActivity { get; set; } = new List<SystemActivityViewModel>();

        public IndexModel(
            ITimeEntryRepository timeEntryRepository,
            IUserRepository userRepository,
            IScheduleRepository scheduleRepository,
            ILogger<IndexModel> logger)
        {
            _timeEntryRepository = timeEntryRepository;
            _userRepository = userRepository;
            _scheduleRepository = scheduleRepository;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return RedirectToPage("/Account/Login");
                }

                // Get user info
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found in database");
                    return RedirectToPage("/Account/Login");
                }

                UserName = $"{user.FirstName} {user.LastName}";
                UserID = userId;

                // Get active time entry
                ActiveTimeEntry = await _timeEntryRepository.GetLatestActiveEntryAsync(userId);

                // Load appropriate data based on user role
                if (User.IsInRole("Employee") || User.IsInRole("1"))
                {
                    await LoadEmployeeTimeData(userId);
                }

                if (User.IsInRole("Manager") || User.IsInRole("2"))
                {
                    await LoadManagerData(userId);
                }

                if (User.IsInRole("Administrator") || User.IsInRole("3"))
                {
                    await LoadAdminData();
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                ErrorMessage = "An error occurred while loading the dashboard.";
                return Page();
            }
        }

        private async Task LoadEmployeeTimeData(string userId)
        {
            // Get data for the current week (Monday to Sunday)
            var today = DateTime.Today;
            var currentDayOfWeek = (int)today.DayOfWeek; // 0 = Sunday, 1 = Monday, etc.

            // Calculate the start of the week (Monday)
            var startOfWeek = today.AddDays(currentDayOfWeek == 0 ? -6 : 1 - currentDayOfWeek);

            // End of week (Sunday)
            var endOfWeek = startOfWeek.AddDays(6);

            _logger.LogInformation($"Loading time entries for user {userId} from {startOfWeek:yyyy-MM-dd} to {endOfWeek:yyyy-MM-dd}");

            // Get time entries for the current week
            RecentTimeEntries = await _timeEntryRepository.GetEntriesByDateRangeAsync(
                userId,
                startOfWeek,
                endOfWeek.AddDays(1) // Include the full end day
            );

            // Calculate weekly hours
            WeeklyHours = RecentTimeEntries
                .Where(te => te.ClockOut.HasValue)
                .Sum(te => te.TotalWorkTime?.TotalHours ?? 0);

            _logger.LogInformation($"Found {RecentTimeEntries.Count()} time entries with total {WeeklyHours} hours for the week");

            // Load upcoming schedules (next 7 days)
            var nextWeek = today.AddDays(7);

            UpcomingSchedules = await _scheduleRepository.GetByUserIdAndDateRangeAsync(
                userId,
                today,
                nextWeek
            );

            // Order by start date and take only the next few
            UpcomingSchedules = UpcomingSchedules
                .Where(s => s.Status != ScheduleStatus.Cancelled) // Don't show cancelled schedules
                .OrderBy(s => s.ShiftStart)
                .Take(5) // Show only the next 5 shifts
                .ToList();
        }

        private async Task LoadManagerData(string managerId)
        {
            // Get all employees assigned to this manager
            var users = await _userRepository.GetAllAsync();
            var employees = users.Where(u => u.Role == UserRole.Employee || u.Role.ToString() == "1").ToList();

            // For a real implementation, you would filter to only show team members for the current manager
            // This is a simplified implementation
            TeamMembers = new List<TeamMemberViewModel>();

            foreach (var employee in employees)
            {
                // Get active time entry for this employee
                var activeEntry = await _timeEntryRepository.GetLatestActiveEntryAsync(employee.Id);
                var isActive = activeEntry != null && !activeEntry.ClockOut.HasValue;

                // Get today's entries for this employee
                var todayEntries = await _timeEntryRepository.GetEntriesByDateRangeAsync(
                    employee.Id,
                    DateTime.Today,
                    DateTime.Today.AddDays(1).AddSeconds(-1)
                );

                // Calculate today's hours
                var todayHours = todayEntries
                    .Where(te => te.ClockOut.HasValue)
                    .Sum(te => te.TotalWorkTime?.TotalHours ?? 0);

                TeamMembers.Add(new TeamMemberViewModel
                {
                    Id = employee.Id,
                    Name = $"{employee.FirstName} {employee.LastName}",
                    IsActive = isActive,
                    TodayHours = todayHours
                });
            }

            TeamMemberCount = TeamMembers.Count;
            ClockedInCount = TeamMembers.Count(m => m.IsActive);

            // Count pending schedules that need approval
            var pendingSchedules = await _scheduleRepository.GetByDateRangeAsync(
                DateTime.Today,
                DateTime.Today.AddDays(14)
            );
            PendingApprovals = pendingSchedules.Count(s => s.Status == ScheduleStatus.Pending);
        }

        private async Task LoadAdminData()
        {
            try
            {
                // Get system-wide statistics
                var users = await _userRepository.GetAllAsync();
                TotalUsers = users.Count();

                // For active sessions and total hours, we need to query separately for each user
                // since our repository doesn't support querying all users at once
                ActiveSessions = 0;
                TotalHoursToday = 0;
                var today = DateTime.Today;

                foreach (var user in users)
                {
                    // Get active time entry for this user
                    var activeEntry = await _timeEntryRepository.GetLatestActiveEntryAsync(user.Id);
                    if (activeEntry != null && activeEntry.Status == TimeEntryStatus.Active && !activeEntry.ClockOut.HasValue)
                    {
                        ActiveSessions++;
                    }

                    // Get today's entries for this user
                    var todayEntries = await _timeEntryRepository.GetEntriesByDateRangeAsync(
                        user.Id,
                        today,
                        today.AddDays(1).AddSeconds(-1)
                    );

                    // Add to total hours
                    TotalHoursToday += todayEntries
                        .Where(te => te.ClockOut.HasValue)
                        .Sum(te => te.TotalWorkTime?.TotalHours ?? 0);
                }

                // Count pending schedules that need approval
                var pendingSchedules = await _scheduleRepository.GetByDateRangeAsync(
                    today,
                    today.AddDays(14)
                );
                PendingApprovals = pendingSchedules.Count(s => s.Status == ScheduleStatus.Pending);

                // Create activity data from recent entries
                RecentSystemActivity = new List<SystemActivityViewModel>();

                // Since we don't have a method to get all recent entries across users,
                // we'll collect them from each user and then sort them
                var allRecentEntries = new List<(TimeEntry Entry, User User)>();

                foreach (var user in users)
                {
                    var userEntries = await _timeEntryRepository.GetByUserIdAsync(user.Id);
                    var recentUserEntries = userEntries.OrderByDescending(e => e.ClockIn).Take(3);
                    allRecentEntries.AddRange(recentUserEntries.Select(e => (e, user)));
                }

                // Take the most recent entries across all users
                var recentSortedEntries = allRecentEntries
                    .OrderByDescending(item => item.Entry.ClockIn)
                    .Take(10);

                foreach (var (entry, user) in recentSortedEntries)
                {
                    var action = entry.ClockOut.HasValue ? "Clock Out" : "Clock In";
                    var details = entry.ClockOut.HasValue
                        ? $"Total time: {Math.Round((entry.ClockOut.Value - entry.ClockIn).TotalHours, 1)} hours"
                        : "Session in progress";

                    RecentSystemActivity.Add(new SystemActivityViewModel
                    {
                        UserName = $"{user.FirstName} {user.LastName}",
                        Action = action,
                        Timestamp = entry.ClockOut ?? entry.ClockIn,
                        Details = details
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin data");
                // Set default values to prevent null reference exceptions
                TotalUsers = 0;
                ActiveSessions = 0;
                TotalHoursToday = 0;
                PendingApprovals = 0;
                RecentSystemActivity = new List<SystemActivityViewModel>();
            }
        }
        public async Task<IActionResult> OnPostClockInAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                var timeEntry = new TimeEntry
                {
                    UserId = userId,
                    ClockIn = DateTime.UtcNow,
                    Status = TimeEntryStatus.Active
                };

                await _timeEntryRepository.AddAsync(timeEntry);
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clock in error");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnGetWeeklyTimeDataAsync(DateTime weekStart)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Calculate week end (7 days from start)
                var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

                // Get time entries for the week
                var entries = await _timeEntryRepository.GetEntriesByDateRangeAsync(userId, weekStart, weekEnd);

                // Create a list for all days in the week
                var dailyEntries = new List<object>();

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = weekStart.AddDays(i).Date;
                    var entriesForDay = entries.Where(e => e.ClockIn.Date == currentDate).ToList();

                    var totalHoursForDay = entriesForDay.Sum(e =>
                        e.TotalWorkTime.HasValue ? e.TotalWorkTime.Value.TotalHours : 0);

                    dailyEntries.Add(new
                    {
                        Date = currentDate,
                        DayOfWeek = currentDate.DayOfWeek.ToString(),
                        Entries = entriesForDay.Select(e => new
                        {
                            Id = e.Id,
                            ClockIn = e.ClockIn,
                            ClockOut = e.ClockOut,
                            TotalHours = e.TotalWorkTime.HasValue ? Math.Round(e.TotalWorkTime.Value.TotalHours, 1) : 0,
                            Status = e.Status.ToString()
                        }).ToList(),
                        TotalHours = Math.Round(totalHoursForDay, 1)
                    });
                }

                // Calculate total weekly hours
                var totalWeeklyHours = dailyEntries.Sum(d => (double)((dynamic)d).TotalHours);

                return new JsonResult(new
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    TotalHours = Math.Round(totalWeeklyHours, 1),
                    DailyEntries = dailyEntries
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weekly time data");
                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
            }
        }

        public async Task<IActionResult> OnPostClockOutAsync()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToPage("/Account/Login");
                }

                var activeEntry = await _timeEntryRepository.GetLatestActiveEntryAsync(userId);

                if (activeEntry != null)
                {
                    activeEntry.ClockOut = DateTime.UtcNow;
                    activeEntry.Status = TimeEntryStatus.Approved;
                    await _timeEntryRepository.UpdateAsync(activeEntry);
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Clock out error");
                return RedirectToPage();
            }
        }
    }

    public class TeamMemberViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public double TodayHours { get; set; }
    }

    public class SystemActivityViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Details { get; set; } = string.Empty;
    }
}