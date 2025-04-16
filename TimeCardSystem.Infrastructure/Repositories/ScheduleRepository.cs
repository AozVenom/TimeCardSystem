using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeCardSystem.API.ViewModels;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Infrastructure.Data;

namespace TimeCardSystem.Infrastructure.Repositories
{
    public class ScheduleRepository : IScheduleRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;

        public ScheduleRepository(
            ApplicationDbContext context,
            IUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<ScheduleItemViewModel>> SearchSchedulesAsync(
    string? employeeId = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    ScheduleStatus? status = null)
        {
            var query = _context.Schedules.AsQueryable();

            if (!string.IsNullOrEmpty(employeeId))
            {
                query = query.Where(s => s.UserId == employeeId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.ShiftStart >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.ShiftStart <= endDate.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            var schedulesWithUserInfo = await query
                .OrderByDescending(s => s.ShiftStart)
                .Select(s => new
                {
                    Schedule = s,
                    UserName = _context.Users
                        .Where(u => u.Id == s.UserId)
                        .Select(u => $"{u.FirstName ?? ""} {u.LastName ?? ""}".Trim())
                        .FirstOrDefault() ?? "Unknown"
                })
                .ToListAsync();

            return schedulesWithUserInfo.Select(x => new ScheduleItemViewModel
            {
                Id = x.Schedule.Id,
                ShiftStart = x.Schedule.ShiftStart,
                ShiftEnd = x.Schedule.ShiftEnd,
                Location = x.Schedule.Location,
                Notes = x.Schedule.Notes,
                Status = x.Schedule.Status,
                BreakDuration = x.Schedule.BreakDuration,
                EmployeeName = x.UserName
            });
        }

        public async Task<Schedule> GetByIdAsync(int id)
        {
            return await _context.Schedules.FindAsync(id);
        }

        public async Task<IEnumerable<Schedule>> GetAllAsync()
        {
            return await _context.Schedules.ToListAsync();
        }

        public async Task<Schedule> AddAsync(Schedule schedule)
        {
            await _context.Schedules.AddAsync(schedule);
            await _context.SaveChangesAsync();
            return schedule;
        }

        public async Task UpdateAsync(Schedule schedule)
        {
            schedule.ModifiedAt = DateTime.UtcNow;
            _context.Schedules.Update(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule != null)
            {
                _context.Schedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Schedule>> GetByUserIdAsync(string userId)
        {
            return await _context.Schedules
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.ShiftStart)
                .ToListAsync();
        }

        public async Task<IEnumerable<Schedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Schedules
                .Where(s => (s.ShiftStart >= startDate && s.ShiftStart <= endDate) ||
                           (s.ShiftEnd >= startDate && s.ShiftEnd <= endDate) ||
                           (s.ShiftStart <= startDate && s.ShiftEnd >= endDate))
                .OrderBy(s => s.ShiftStart)
                .ToListAsync();
        }

        public async Task<IEnumerable<Schedule>> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Schedules
                .Where(s => s.UserId == userId &&
                          ((s.ShiftStart >= startDate && s.ShiftStart <= endDate) ||
                           (s.ShiftEnd >= startDate && s.ShiftEnd <= endDate) ||
                           (s.ShiftStart <= startDate && s.ShiftEnd >= endDate)))
                .OrderBy(s => s.ShiftStart)
                .ToListAsync();
        }

        public async Task<Schedule> GetUserScheduleForDateAsync(string userId, DateTime date)
        {
            // Get the beginning and end of the specified date
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1).AddSeconds(-1);

            return await _context.Schedules
                .Where(s => s.UserId == userId &&
                          ((s.ShiftStart >= dayStart && s.ShiftStart <= dayEnd) ||
                           (s.ShiftEnd >= dayStart && s.ShiftEnd <= dayEnd) ||
                           (s.ShiftStart <= dayStart && s.ShiftEnd >= dayEnd)))
                .OrderBy(s => s.ShiftStart)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Schedule>> GetTeamSchedulesAsync(IEnumerable<string> teamMemberIds, DateTime startDate, DateTime endDate)
        {
            return await _context.Schedules
                .Where(s => teamMemberIds.Contains(s.UserId) &&
                          ((s.ShiftStart >= startDate && s.ShiftStart <= endDate) ||
                           (s.ShiftEnd >= startDate && s.ShiftEnd <= endDate) ||
                           (s.ShiftStart <= startDate && s.ShiftEnd >= endDate)))
                .OrderBy(s => s.ShiftStart)
                .ThenBy(s => s.UserId)
                .ToListAsync();
        }

        public async Task<bool> HasOverlappingScheduleAsync(string userId, DateTime shiftStart, DateTime shiftEnd, int? excludeScheduleId = null)
        {
            var query = _context.Schedules
                .Where(s => s.UserId == userId &&
                          ((shiftStart >= s.ShiftStart && shiftStart < s.ShiftEnd) ||
                           (shiftEnd > s.ShiftStart && shiftEnd <= s.ShiftEnd) ||
                           (shiftStart <= s.ShiftStart && shiftEnd >= s.ShiftEnd)));

            if (excludeScheduleId.HasValue)
            {
                query = query.Where(s => s.Id != excludeScheduleId.Value);
            }

            return await query.AnyAsync();
        }
    }
}