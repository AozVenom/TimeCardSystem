using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeCardSystem.Core.Models
{
    public class TimeEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        // Added for reporting - maps to User's EmployeeId
        public int EmployeeId { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public DateTime ClockIn { get; set; }

        public DateTime? ClockOut { get; set; }

        public TimeSpan? BreakDuration { get; set; }

        public DateTime? LunchClockIn { get; set; }

        public DateTime? LunchClockOut { get; set; }

        // Calculate regular hours (default to 8-hour workday)
        [NotMapped]
        public double RegularHours
        {
            get
            {
                if (!TotalWorkTime.HasValue) return 0;
                double totalHours = TotalWorkTime.Value.TotalHours;
                return Math.Min(totalHours, 8); // First 8 hours are regular
            }
        }

        // Calculate overtime hours (anything over 8 hours)
        [NotMapped]
        public double OvertimeHours
        {
            get
            {
                if (!TotalWorkTime.HasValue) return 0;
                double totalHours = TotalWorkTime.Value.TotalHours;
                return Math.Max(0, totalHours - 8); // Anything over 8 hours is overtime
            }
        }

        // Description field for task information
        public string Description { get; set; } = string.Empty;

        // Notes field for additional context
        public string Notes { get; set; } = string.Empty;

        [NotMapped]
        public TimeSpan? TotalWorkTime
        {
            get
            {
                if (!ClockOut.HasValue) return null;

                var totalTime = ClockOut.Value - ClockIn;

                // Subtract lunch break if applicable
                if (LunchClockIn.HasValue && LunchClockOut.HasValue)
                {
                    totalTime -= (LunchClockOut.Value - LunchClockIn.Value);
                }

                // Subtract additional breaks
                return BreakDuration.HasValue
                    ? totalTime - BreakDuration.Value
                    : totalTime;
            }
        }

        public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Active;
    }

    public enum TimeEntryStatus
    {
        Active = 1,
        Approved = 2,
        Rejected = 3,
        Edited = 4
    }
}