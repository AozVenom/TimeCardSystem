
using Microsoft.AspNetCore.Identity;
using System;

namespace TimeCardSystem.Core.Models
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public UserRole Role { get; set; } = UserRole.Employee;
    }

    public enum UserRole
    {
        Employee = 1,
        Manager = 2,
        Administrator = 3
    }
}