using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Core.Interfaces
{
    public interface IUserRepository
    {
        // Existing methods
        Task<User> GetByIdAsync(string id);
        Task<User> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> AddAsync(User user);
        Task UpdateAsync(User user);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task<IEnumerable<User>> GetEmployeesAsync();

        // New methods for EmployeeId support
        Task<User> GetByEmployeeIdAsync(int employeeId);
        Task<string> GetUserIdFromEmployeeIdAsync(int employeeId);
        Task<int?> GetEmployeeIdFromUserIdAsync(string userId);
        Task<User> CreateUserAsync(User user, string password);
        Task<bool> IsEmployeeIdUniqueAsync(int employeeId, string excludeUserId = null);
    }

    public interface ITimeEntryRepository
    {
        Task<TimeEntry> GetByIdAsync(int id);
        Task<IEnumerable<TimeEntry>> GetAllAsync();
        Task<IEnumerable<TimeEntry>> GetByUserIdAsync(string userId);

        // Added for reporting - get entries by employee ID and date range
        Task<IEnumerable<TimeEntry>> GetByEmployeeAndDateRangeAsync(int employeeId, DateTime startDate, DateTime endDate);

        Task<TimeEntry> AddAsync(TimeEntry timeEntry);
        Task UpdateAsync(TimeEntry timeEntry);
        Task<bool> DeleteAsync(int id);

        // Additional methods for time card operations
        Task<TimeEntry> GetActiveEntryForUserAsync(string userId);
        Task<IEnumerable<TimeEntry>> GetTimeEntriesForPayPeriodAsync(string userId, DateTime startDate, DateTime endDate);
        Task<TimeEntry> ClockInAsync(string userId);
        Task<TimeEntry> ClockOutAsync(int timeEntryId);
        Task<TimeEntry> StartLunchAsync(int timeEntryId);
        Task<TimeEntry> EndLunchAsync(int timeEntryId);

        // Existing methods
        Task<TimeEntry> GetLatestActiveEntryAsync(string userId);
        Task<IEnumerable<TimeEntry>> GetEntriesByDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    }
}