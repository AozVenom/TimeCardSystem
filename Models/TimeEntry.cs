using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimeCardSystem.Models
{
    public class TimeEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        [Required]
        public DateTime ClockIn { get; set; }

        public DateTime? ClockOut { get; set; }

        public TimeSpan? BreakDuration { get; set; }

        public DateTime? LunchClockIn { get; set; }

        public DateTime? LunchClockOut { get; set; }

        [NotMapped]
        public TimeSpan? TotalWorkTime
        {
            get
            {
                if (!ClockOut.HasValue) return null;

                var totalTime = ClockOut.Value - ClockIn;
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