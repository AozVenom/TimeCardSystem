using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeCardSystem.API.ViewModels;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Core.Interfaces
{
    public interface IScheduleRepository
    {
        // Basic CRUD operations
        Task<Schedule> GetByIdAsync(int id);
        Task<IEnumerable<Schedule>> GetAllAsync();
        Task<Schedule> AddAsync(Schedule schedule);
        Task UpdateAsync(Schedule schedule);
        Task DeleteAsync(int id);
        Task<IEnumerable<ScheduleItemViewModel>> SearchSchedulesAsync(
        string? employeeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        ScheduleStatus? status = null
    );

        // Specialized queries
        Task<IEnumerable<Schedule>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Schedule>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<Schedule>> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
        Task<Schedule> GetUserScheduleForDateAsync(string userId, DateTime date);
        Task<IEnumerable<Schedule>> GetTeamSchedulesAsync(IEnumerable<string> teamMemberIds, DateTime startDate, DateTime endDate);
        Task<bool> HasOverlappingScheduleAsync(string userId, DateTime shiftStart, DateTime shiftEnd, int? excludeScheduleId = null);
    }
}