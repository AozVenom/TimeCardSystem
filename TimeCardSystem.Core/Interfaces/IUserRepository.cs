using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Core.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetByIdAsync(string id);
        Task<User> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<User>> GetEmployeesAsync();
    }

    public interface ITimeEntryRepository
    {
        Task<TimeEntry> GetByIdAsync(int id);
        Task<IEnumerable<TimeEntry>> GetByUserIdAsync(string userId);
        Task<TimeEntry> GetLatestActiveEntryAsync(string userId);
        Task<TimeEntry> AddAsync(TimeEntry timeEntry);
        Task UpdateAsync(TimeEntry timeEntry);
        Task DeleteAsync(int id);
        Task<IEnumerable<TimeEntry>> GetEntriesByDateRangeAsync(
            string userId,
            DateTime startDate,
            DateTime endDate
        );
    }
}