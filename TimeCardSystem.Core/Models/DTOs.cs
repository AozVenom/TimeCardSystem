﻿using System;
using System.ComponentModel.DataAnnotations;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Core.Dtos
{
    public class UserRegistrationDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Employee;
    }

    public class TimeEntryDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public TimeSpan? BreakDuration { get; set; }
        public DateTime? LunchClockIn { get; set; }
        public DateTime? LunchClockOut { get; set; }
        public TimeSpan? TotalWorkTime { get; set; }
        public TimeEntryStatus Status { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }

    public class ScheduleDto
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;  // For displaying employee name

        [Required]
        public DateTime ShiftStart { get; set; }

        [Required]
        public DateTime ShiftEnd { get; set; }

        public ScheduleStatus Status { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public TimeSpan? BreakDuration { get; set; }

        public string CreatedById { get; set; } = string.Empty;

        public string CreatedByName { get; set; } = string.Empty;  // For displaying manager name

        public DateTime CreatedAt { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public double TotalHours => (ShiftEnd - ShiftStart).TotalHours - (BreakDuration?.TotalHours ?? 0);
    }

    public class CreateScheduleDto
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime ShiftStart { get; set; }

        [Required]
        public DateTime ShiftEnd { get; set; }

        public string Location { get; set; } = "Main Office";

        public string? Notes { get; set; }

        public TimeSpan? BreakDuration { get; set; }

        // New property for lunch dropdown
        public string LunchOption { get; set; } = "30";

        // Calculate BreakDuration based on LunchOption
        public TimeSpan? GetBreakDuration() => LunchOption switch
        {
            "30" => TimeSpan.FromMinutes(30),
            "60" => TimeSpan.FromHours(1),
            _ => null
        };
    }

    public class UpdateScheduleDto
    {
        public DateTime? ShiftStart { get; set; }

        public DateTime? ShiftEnd { get; set; }

        public ScheduleStatus? Status { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public TimeSpan? BreakDuration { get; set; }
    }
}