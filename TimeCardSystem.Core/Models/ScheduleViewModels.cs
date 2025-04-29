using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.ViewModels
{
    public class ScheduleCreateViewModel
    {
        [Required(ErrorMessage = "Employee must be selected")]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shift start time is required")]
        [Display(Name = "Shift Start")]
        [DataType(DataType.DateTime)]
        public DateTime ShiftStart { get; set; }

        [Required(ErrorMessage = "Shift end time is required")]
        [Display(Name = "Shift End")]
        [DataType(DataType.DateTime)]
        public DateTime ShiftEnd { get; set; }

        // Remove HasLunch property and add LunchOption
        [Display(Name = "Lunch Break")]
        public string LunchOption { get; set; } = "30";  // Default to 30 minutes

        [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters")]
        [Display(Name = "Additional Notes")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100, ErrorMessage = "Location cannot be longer than 100 characters")]
        [Display(Name = "Work Location")]
        public string Location { get; set; } = "Main Office";

        [Display(Name = "Status")]
        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

        // Calculate BreakDuration based on LunchOption
        public TimeSpan? BreakDuration => LunchOption switch
        {
            "30" => TimeSpan.FromMinutes(30),
            "60" => TimeSpan.FromHours(1),
            _ => null
        };

        // For backwards compatibility
        public bool HasLunch
        {
            get => LunchOption != "None";
            set => LunchOption = value ? "30" : "None";
        }

        public Schedule ToScheduleModel(string createdById)
        {
            return new Schedule
            {
                UserId = EmployeeId,
                ShiftStart = ShiftStart,
                ShiftEnd = ShiftEnd,
                HasLunch = LunchOption != "None",
                Notes = Notes,
                Location = Location,
                CreatedById = createdById,
                Status = Status,
                BreakDuration = BreakDuration,
                LunchDuration = BreakDuration ?? TimeSpan.Zero
            };
        }
    }

    public class EmployeeScheduleViewModel
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<DailyScheduleViewModel> DailySchedules { get; set; } = new List<DailyScheduleViewModel>();
    }

    public class DailyScheduleViewModel
    {
        public DateTime? Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public List<ScheduleItemViewModel> Schedules { get; set; } = new List<ScheduleItemViewModel>();
        public bool HasSchedule => Schedules != null && Schedules.Count > 0;
    }

    public class ScheduleItemViewModel
    {
        public int Id { get; set; }
        public DateTime ShiftStart { get; set; }
        public DateTime ShiftEnd { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public ScheduleStatus Status { get; set; }

        // Make these settable
        public string StatusName { get; set; } = string.Empty;
        public TimeSpan? BreakDuration { get; set; }
        public double TotalHours { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        // Make these settable
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
    }

    public class ScheduleSearchViewModel
    {
        [Display(Name = "Employee")]
        public string? EmployeeId { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Status")]
        public ScheduleStatus? Status { get; set; }
    }
}