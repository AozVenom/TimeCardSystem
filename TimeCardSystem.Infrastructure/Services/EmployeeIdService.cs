using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Infrastructure.Data;

namespace TimeCardSystem.Infrastructure.Services
{
    public class EmployeeIdService : IEmployeeIdService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EmployeeIdService> _logger;

        public EmployeeIdService(
            ApplicationDbContext dbContext,
            ILogger<EmployeeIdService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task GenerateEmployeeIdsForExistingUsersAsync()
        {
            try
            {
                // Get all users without an EmployeeId (or with EmployeeId = 0)
                var users = await _dbContext.Users
                    .Where(u => u.EmployeeId == 0)
                    .ToListAsync();

                if (!users.Any())
                {
                    _logger.LogInformation("No users found that need EmployeeId generation");
                    return;
                }

                _logger.LogInformation($"Generating EmployeeIds for {users.Count} users");

                // Keep track of used IDs to avoid duplicates
                var usedIds = new HashSet<int>();

                // Get existing EmployeeIds to avoid duplicates
                var existingIds = await _dbContext.Users
                    .Where(u => u.EmployeeId != 0)
                    .Select(u => u.EmployeeId)
                    .ToListAsync();

                foreach (var id in existingIds)
                {
                    usedIds.Add(id);
                }

                foreach (var user in users)
                {
                    var employeeCode = user.EmployeeCode;

                    // Try to convert the code to an integer
                    if (int.TryParse(employeeCode, out int generatedId))
                    {
                        // If the ID is already used, increment until we find an available one
                        while (usedIds.Contains(generatedId))
                        {
                            generatedId++;
                        }

                        // Assign and track the ID
                        user.EmployeeId = generatedId;
                        usedIds.Add(generatedId);
                    }
                    else
                    {
                        // If conversion fails, generate a random unique ID
                        int randomId = new Random().Next(100000, 999999);
                        while (usedIds.Contains(randomId))
                        {
                            randomId = new Random().Next(100000, 999999);
                        }

                        user.EmployeeId = randomId;
                        usedIds.Add(randomId);

                        _logger.LogWarning($"Could not generate EmployeeId from code for user {user.Id}, using random ID {randomId}");
                    }
                }

                // Save changes
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Successfully generated and saved EmployeeIds for {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating EmployeeIds for existing users");
                throw;
            }
        }

        public async Task<int> GenerateEmployeeIdForUserAsync(User user)
        {
            // Get the employee code from the user
            var employeeCode = user.EmployeeCode;

            // Check if we can parse it to an integer
            if (int.TryParse(employeeCode, out int generatedId))
            {
                // Check if this ID is already in use
                bool idExists = await _dbContext.Users
                    .AnyAsync(u => u.EmployeeId == generatedId && u.Id != user.Id);

                if (!idExists)
                {
                    return generatedId;
                }

                // If ID exists, find the next available ID
                while (await _dbContext.Users.AnyAsync(u => u.EmployeeId == generatedId))
                {
                    generatedId++;
                }

                return generatedId;
            }

            // If we can't parse the code, generate a random ID
            int randomId = new Random().Next(100000, 999999);
            while (await _dbContext.Users.AnyAsync(u => u.EmployeeId == randomId))
            {
                randomId = new Random().Next(100000, 999999);
            }

            return randomId;
        }

        public async Task UpdateTimeEntriesWithEmployeeIdsAsync()
        {
            try
            {
                // Get all users with their EmployeeId
                var userIdMap = await _dbContext.Users
                    .Where(u => u.EmployeeId != 0)
                    .Select(u => new { u.Id, u.EmployeeId })
                    .ToDictionaryAsync(u => u.Id, u => u.EmployeeId);

                // Get all time entries that need updating
                var timeEntriesToUpdate = await _dbContext.TimeEntries
                    .Where(t => userIdMap.ContainsKey(t.UserId) &&
                           (t.EmployeeId == null || t.EmployeeId == 0 || t.EmployeeId != userIdMap[t.UserId]))
                    .ToListAsync();

                if (!timeEntriesToUpdate.Any())
                {
                    _logger.LogInformation("No time entries need updating with EmployeeIds");
                    return;
                }

                _logger.LogInformation($"Updating {timeEntriesToUpdate.Count} time entries with EmployeeIds");

                // Update each time entry with the correct EmployeeId
                foreach (var entry in timeEntriesToUpdate)
                {
                    if (userIdMap.TryGetValue(entry.UserId, out int employeeId))
                    {
                        entry.EmployeeId = employeeId;
                    }
                }

                // Save changes
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Successfully updated {timeEntriesToUpdate.Count} time entries with EmployeeIds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entries with EmployeeIds");
                throw;
            }
        }
    }
}