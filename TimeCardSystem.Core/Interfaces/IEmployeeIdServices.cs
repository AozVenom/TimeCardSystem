using System.Threading.Tasks;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Core.Interfaces
{
    /// <summary>
    /// Service for generating and managing employee IDs
    /// </summary>
    public interface IEmployeeIdService
    {
        /// <summary>
        /// Generates employee IDs for all existing users that don't have them
        /// </summary>
        Task GenerateEmployeeIdsForExistingUsersAsync();

        /// <summary>
        /// Generates a unique employee ID for a specific user
        /// </summary>
        /// <param name="user">The user to generate an ID for</param>
        /// <returns>The generated employee ID</returns>
        Task<int> GenerateEmployeeIdForUserAsync(User user);

        /// <summary>
        /// Updates time entries with the correct employee IDs based on user mappings
        /// </summary>
        Task UpdateTimeEntriesWithEmployeeIdsAsync();
    }
}