using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeCardSystem.Core.Models
{
    public class Schedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(450)] // Recommended for foreign key strings
        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime ShiftStart { get; set; }

        [Required]
        public DateTime ShiftEnd { get; set; }

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Pending;

        [Required] // Explicitly mark as required
        public bool HasLunch { get; set; }

        public TimeSpan LunchDuration { get; set; } = TimeSpan.FromMinutes(30);

        [StringLength(500)] // Optional: limit note length
        public string? Notes { get; set; }

        [Required]
        [StringLength(100)] // Limit location length
        public string Location { get; set; } = "Main Office";

        // Existing calculated properties are perfect
        public TimeSpan TotalShiftDuration =>
            TimeSpan.FromHours(8) + (HasLunch ? LunchDuration : TimeSpan.Zero);

        public TimeSpan? BreakDuration { get; set; }

        [Required]
        [StringLength(450)] // Recommended for foreign key strings
        public string CreatedById { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        [NotMapped]
        public TimeSpan TotalScheduledTime
        {
            get
            {
                var total = ShiftEnd - ShiftStart;
                return BreakDuration.HasValue ? total - BreakDuration.Value : total;
            }
        }
    }
    public enum ScheduleStatus
    {
        Pending = 1,
        Published = 2,
        Cancelled = 3,
        Completed = 4
    }
}