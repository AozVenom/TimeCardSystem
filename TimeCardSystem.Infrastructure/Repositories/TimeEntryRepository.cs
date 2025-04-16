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

        public TimeEntryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TimeEntry> GetByIdAsync(int id)
        {
            return await _context.TimeEntries.FindAsync(id);
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

        public async Task<TimeEntry> AddAsync(TimeEntry timeEntry)
        {
            await _context.TimeEntries.AddAsync(timeEntry);
            await _context.SaveChangesAsync();
            return timeEntry;
        }

        public async Task UpdateAsync(TimeEntry timeEntry)
        {
            _context.TimeEntries.Update(timeEntry);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var timeEntry = await GetByIdAsync(id);
            if (timeEntry != null)
            {
                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();
            }
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
    }
}