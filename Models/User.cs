using Microsoft.AspNetCore.Identity;

namespace TimeCardSystem.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public UserRole Role { get; set; } = UserRole.Employee;
        public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
    }

    public enum UserRole
    {
        Employee = 1,
        Manager = 2,
        Administrator = 3
    }
}