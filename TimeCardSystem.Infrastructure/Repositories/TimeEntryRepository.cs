using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Infrastructure.Data;

namespace TimeCardSystem.Infrastructure.Repositories
{
    public class TimeEntryRepository : ITimeEntryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;

        public TimeEntryRepository(
            ApplicationDbContext context,
            IUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
        }

        public async Task<TimeEntry> GetByIdAsync(int id)
        {
            return await _context.TimeEntries.FindAsync(id);
        }

        public async Task<IEnumerable<TimeEntry>> GetAllAsync()
        {
            return await _context.TimeEntries
                .OrderByDescending(te => te.ClockIn)
                .ToListAsync();
        }

        public async Task<IEnumerable<TimeEntry>> GetByUserIdAsync(string userId)
        {
            return await _context.TimeEntries
                .Where(te => te.UserId == userId)
                .OrderByDescending(te => te.ClockIn)
                .ToListAsync();
        }

        public async Task<TimeEntry> GetLatestActiveEntryAsync(string userId)
        {
            return await _context.TimeEntries
                .Where(te => te.UserId == userId && te.Status == TimeEntryStatus.Active)
                .OrderByDescending(te => te.ClockIn)
                .FirstOrDefaultAsync();
        }

        public async Task<TimeEntry> GetActiveEntryForUserAsync(string userId)
        {
            return await _context.TimeEntries
                .Where(te => te.UserId == userId &&
                       te.Status == TimeEntryStatus.Active &&
                       te.ClockOut == null)
                .OrderByDescending(te => te.ClockIn)
                .FirstOrDefaultAsync();
        }

        public async Task<TimeEntry> AddAsync(TimeEntry timeEntry)
        {
            // If Date is not set, use the date part of ClockIn
            if (timeEntry.Date == default)
            {
                timeEntry.Date = timeEntry.ClockIn.Date;
            }

            // If EmployeeId is not set, lookup from UserId
            if (timeEntry.EmployeeId == 0 && !string.IsNullOrEmpty(timeEntry.UserId))
            {
                var employeeId = await _userRepository.GetEmployeeIdFromUserIdAsync(timeEntry.UserId);
                if (employeeId.HasValue)
                {
                    timeEntry.EmployeeId = employeeId.Value;
                }
            }

            await _context.TimeEntries.AddAsync(timeEntry);
            await _context.SaveChangesAsync();
            return timeEntry;
        }

        public async Task UpdateAsync(TimeEntry timeEntry)
        {
            // Make sure the Date field is set
            if (timeEntry.Date == default)
            {
                timeEntry.Date = timeEntry.ClockIn.Date;
            }

            _context.TimeEntries.Update(timeEntry);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var timeEntry = await GetByIdAsync(id);
            if (timeEntry == null)
                return false;

            _context.TimeEntries.Remove(timeEntry);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<TimeEntry>> GetEntriesByDateRangeAsync(
            string userId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.TimeEntries
                .Where(te => te.UserId == userId &&
                             te.ClockIn >= startDate &&
                             te.ClockIn <= endDate)
                .OrderBy(te => te.ClockIn)
                .ToListAsync();
        }

        // New methods for reporting

        public async Task<IEnumerable<TimeEntry>> GetByEmployeeAndDateRangeAsync(
            int employeeId,
            DateTime startDate,
            DateTime endDate)
        {
            var entries = await _context.TimeEntries
                .Where(te => te.EmployeeId == employeeId &&
                             te.Date >= startDate.Date &&
                             te.Date <= endDate.Date)
                .OrderBy(te => te.Date)
                .ThenBy(te => te.ClockIn)
                .ToListAsync();

            return entries;
        }

        public async Task<IEnumerable<TimeEntry>> GetTimeEntriesForPayPeriodAsync(
            string userId,
            DateTime startDate,
            DateTime endDate)
        {
            return await _context.TimeEntries
                .Where(te => te.UserId == userId &&
                             te.Date >= startDate.Date &&
                             te.Date <= endDate.Date)
                .OrderBy(te => te.Date)
                .ToListAsync();
        }

        public async Task<TimeEntry> ClockInAsync(string userId)
        {
            // Check if user already has an active entry
            var activeEntry = await GetActiveEntryForUserAsync(userId);
            if (activeEntry != null)
            {
                throw new InvalidOperationException("User already has an active time entry.");
            }

            // Get the employee ID from the user ID
            var employeeId = await _userRepository.GetEmployeeIdFromUserIdAsync(userId);
            if (!employeeId.HasValue)
            {
                throw new InvalidOperationException("Could not find employee ID for user.");
            }

            // Create a new time entry
            var now = DateTime.Now;
            var timeEntry = new TimeEntry
            {
                UserId = userId,
                EmployeeId = employeeId.Value,
                Date = now.Date,
                ClockIn = now,
                Status = TimeEntryStatus.Active
            };

            return await AddAsync(timeEntry);
        }

        public async Task<TimeEntry> ClockOutAsync(int timeEntryId)
        {
            var timeEntry = await GetByIdAsync(timeEntryId);
            if (timeEntry == null)
            {
                throw new KeyNotFoundException($"Time entry with ID {timeEntryId} not found.");
            }

            if (timeEntry.ClockOut.HasValue)
            {
                throw new InvalidOperationException("Time entry is already clocked out.");
            }

            timeEntry.ClockOut = DateTime.Now;

            await UpdateAsync(timeEntry);
            return timeEntry;
        }

        public async Task<TimeEntry> StartLunchAsync(int timeEntryId)
        {
            var timeEntry = await GetByIdAsync(timeEntryId);
            if (timeEntry == null)
            {
                throw new KeyNotFoundException($"Time entry with ID {timeEntryId} not found.");
            }

            if (timeEntry.LunchClockIn.HasValue)
            {
                throw new InvalidOperationException("Lunch break already started.");
            }

            timeEntry.LunchClockIn = DateTime.Now;

            await UpdateAsync(timeEntry);
            return timeEntry;
        }

        public async Task<TimeEntry> EndLunchAsync(int timeEntryId)
        {
            var timeEntry = await GetByIdAsync(timeEntryId);
            if (timeEntry == null)
            {
                throw new KeyNotFoundException($"Time entry with ID {timeEntryId} not found.");
            }

            if (!timeEntry.LunchClockIn.HasValue)
            {
                throw new InvalidOperationException("Lunch break not started.");
            }

            if (timeEntry.LunchClockOut.HasValue)
            {
                throw new InvalidOperationException("Lunch break already ended.");
            }

            timeEntry.LunchClockOut = DateTime.Now;

            await UpdateAsync(timeEntry);
            return timeEntry;
        }
    }
}