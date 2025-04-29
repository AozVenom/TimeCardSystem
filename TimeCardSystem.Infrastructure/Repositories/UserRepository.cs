using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;
using TimeCardSystem.Infrastructure.Data;

namespace TimeCardSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmployeeIdService _employeeIdService;

        public UserRepository(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IEmployeeIdService employeeIdService)
        {
            _context = context;
            _userManager = userManager;
            _employeeIdService = employeeIdService;
        }

        // Existing methods

        public async Task<User> GetByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> AddAsync(User user)
        {
            // Generate an EmployeeId for the new user
            user.EmployeeId = await _employeeIdService.GenerateEmployeeIdForUserAsync(user);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<User>> GetEmployeesAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.Employee)
                .ToListAsync();
        }

        // New methods for EmployeeId support

        /// <summary>
        /// Gets a user by their employee ID
        /// </summary>
        public async Task<User> GetByEmployeeIdAsync(int employeeId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        }

        /// <summary>
        /// Gets the UserId string from an EmployeeId
        /// </summary>
        public async Task<string> GetUserIdFromEmployeeIdAsync(int employeeId)
        {
            var user = await GetByEmployeeIdAsync(employeeId);
            return user?.Id;
        }

        /// <summary>
        /// Gets the EmployeeId from a UserId string
        /// </summary>
        public async Task<int?> GetEmployeeIdFromUserIdAsync(string userId)
        {
            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync();

            return user != 0 ? user : null;
        }

        /// <summary>
        /// Create a new user with Identity and an automatically assigned EmployeeId
        /// </summary>
        public async Task<User> CreateUserAsync(User user, string password)
        {
            // Generate an EmployeeId for the new user
            user.EmployeeId = await _employeeIdService.GenerateEmployeeIdForUserAsync(user);

            // Create the user with Identity
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return user;
        }

        /// <summary>
        /// Validate that the EmployeeId is unique
        /// </summary>
        public async Task<bool> IsEmployeeIdUniqueAsync(int employeeId, string excludeUserId = null)
        {
            if (string.IsNullOrEmpty(excludeUserId))
            {
                return !await _context.Users.AnyAsync(u => u.EmployeeId == employeeId);
            }
            else
            {
                return !await _context.Users.AnyAsync(u => u.EmployeeId == employeeId && u.Id != excludeUserId);
            }
        }
    }
}