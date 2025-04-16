using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimeCardSystem.Core.Dtos;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ScheduleController : ControllerBase
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<ScheduleController> _logger;

        public ScheduleController(
            IScheduleRepository scheduleRepository,
            IUserRepository userRepository,
            ILogger<ScheduleController> logger)
        {
            _scheduleRepository = scheduleRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpGet("check-conflicts")]
        public async Task<IActionResult> CheckConflicts(string userId, DateTime shiftStart, DateTime shiftEnd, int? excludeId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Employee ID is required");
            }

            bool hasConflict = await _scheduleRepository.HasOverlappingScheduleAsync(
                userId, shiftStart, shiftEnd, excludeId);

            return Ok(new { hasConflict });
        }

        [HttpPost]
        [Authorize(Roles = "Manager,Administrator,2,3")]
        public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleDto createDto)
        {
            try
            {
                // Check for schedule conflicts
                bool hasConflict = await _scheduleRepository.HasOverlappingScheduleAsync(
                    createDto.UserId,
                    createDto.ShiftStart,
                    createDto.ShiftEnd);

                if (hasConflict)
                {
                    return BadRequest("Employee already has a schedule that overlaps with this time period.");
                }

                // Get manager/admin ID
                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var schedule = new Schedule
                {
                    UserId = createDto.UserId,
                    ShiftStart = createDto.ShiftStart,
                    ShiftEnd = createDto.ShiftEnd,
                    Location = createDto.Location,
                    Notes = createDto.Notes,
                    BreakDuration = createDto.BreakDuration,
                    Status = ScheduleStatus.Pending,
                    CreatedById = managerId,
                    CreatedAt = DateTime.UtcNow
                };

                await _scheduleRepository.AddAsync(schedule);

                var employee = await _userRepository.GetByIdAsync(createDto.UserId);
                var manager = await _userRepository.GetByIdAsync(managerId);

                return CreatedAtAction(
                    nameof(GetScheduleById),
                    new { id = schedule.Id },
                    new ScheduleDto
                    {
                        Id = schedule.Id,
                        UserId = schedule.UserId,
                        UserName = $"{employee?.FirstName} {employee?.LastName}",
                        ShiftStart = schedule.ShiftStart,
                        ShiftEnd = schedule.ShiftEnd,
                        Status = schedule.Status,
                        Location = schedule.Location,
                        Notes = schedule.Notes,
                        BreakDuration = schedule.BreakDuration,
                        CreatedById = schedule.CreatedById,
                        CreatedByName = $"{manager?.FirstName} {manager?.LastName}",
                        CreatedAt = schedule.CreatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating schedule");
                return StatusCode(500, "An error occurred while creating the schedule.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleDto>> GetScheduleById(int id)
        {
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)
                {
                    return NotFound();
                }

                // Security: Check if the user is the employee, the creator, or has a manager/admin role
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Administrator") ||
                                       User.IsInRole("2") || User.IsInRole("3");

                if (schedule.UserId != userId && schedule.CreatedById != userId && !isManagerOrAdmin)
                {
                    return Forbid();
                }

                var employee = await _userRepository.GetByIdAsync(schedule.UserId);
                var creator = await _userRepository.GetByIdAsync(schedule.CreatedById);

                return Ok(new ScheduleDto
                {
                    Id = schedule.Id,
                    UserId = schedule.UserId,
                    UserName = $"{employee?.FirstName} {employee?.LastName}",
                    ShiftStart = schedule.ShiftStart,
                    ShiftEnd = schedule.ShiftEnd,
                    Status = schedule.Status,
                    Location = schedule.Location,
                    Notes = schedule.Notes,
                    BreakDuration = schedule.BreakDuration,
                    CreatedById = schedule.CreatedById,
                    CreatedByName = $"{creator?.FirstName} {creator?.LastName}",
                    CreatedAt = schedule.CreatedAt,
                    ModifiedAt = schedule.ModifiedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedule {id}");
                return StatusCode(500, "An error occurred while retrieving the schedule.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetUserSchedules(string userId)
        {
            try
            {
                // Security: Check if the user is the employee or has a manager/admin role
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Administrator") ||
                                       User.IsInRole("2") || User.IsInRole("3");

                if (userId != currentUserId && !isManagerOrAdmin)
                {
                    return Forbid();
                }

                var schedules = await _scheduleRepository.GetByUserIdAsync(userId);
                var employee = await _userRepository.GetByIdAsync(userId);

                var scheduleDtos = new List<ScheduleDto>();
                foreach (var schedule in schedules)
                {
                    var creator = await _userRepository.GetByIdAsync(schedule.CreatedById);
                    scheduleDtos.Add(new ScheduleDto
                    {
                        Id = schedule.Id,
                        UserId = schedule.UserId,
                        UserName = $"{employee?.FirstName} {employee?.LastName}",
                        ShiftStart = schedule.ShiftStart,
                        ShiftEnd = schedule.ShiftEnd,
                        Status = schedule.Status,
                        Location = schedule.Location,
                        Notes = schedule.Notes,
                        BreakDuration = schedule.BreakDuration,
                        CreatedById = schedule.CreatedById,
                        CreatedByName = $"{creator?.FirstName} {creator?.LastName}",
                        CreatedAt = schedule.CreatedAt,
                        ModifiedAt = schedule.ModifiedAt
                    });
                }

                return Ok(scheduleDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving schedules for user {userId}");
                return StatusCode(500, "An error occurred while retrieving schedules.");
            }
        }

        [HttpGet("weekly")]
        public async Task<ActionResult<object>> GetWeeklySchedules([FromQuery] DateTime weekStart, [FromQuery] string userId = null)
        {
            try
            {
                // If no userId provided, use the current user's ID
                if (string.IsNullOrEmpty(userId))
                {
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }
                else
                {
                    // Security: Check if trying to access another user's schedule
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var isManagerOrAdmin = User.IsInRole("Manager") || User.IsInRole("Administrator") ||
                                          User.IsInRole("2") || User.IsInRole("3");

                    if (userId != currentUserId && !isManagerOrAdmin)
                    {
                        return Forbid();
                    }
                }

                // Calculate week end (7 days from start)
                var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

                // Get schedules for the week
                var schedules = await _scheduleRepository.GetByUserIdAndDateRangeAsync(userId, weekStart, weekEnd);
                var employee = await _userRepository.GetByIdAsync(userId);

                // Create a list for all days in the week
                var dailySchedules = new List<object>();

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = weekStart.AddDays(i).Date;
                    var scheduleForDay = schedules.Where(s =>
                        (s.ShiftStart.Date <= currentDate && s.ShiftEnd.Date >= currentDate) ||
                        s.ShiftStart.Date == currentDate ||
                        s.ShiftEnd.Date == currentDate
                    ).ToList();

                    var schedulesForDayDto = scheduleForDay.Select(s => new
                    {
                        Id = s.Id,
                        ShiftStart = s.ShiftStart,
                        ShiftEnd = s.ShiftEnd,
                        Location = s.Location,
                        Status = s.Status.ToString(),
                        Notes = s.Notes,
                        TotalHours = (s.ShiftEnd - s.ShiftStart).TotalHours - (s.BreakDuration?.TotalHours ?? 0)
                    }).ToList();

                    dailySchedules.Add(new
                    {
                        Date = currentDate,
                        DayOfWeek = currentDate.DayOfWeek.ToString(),
                        Schedules = schedulesForDayDto,
                        HasSchedule = schedulesForDayDto.Any()
                    });
                }

                // Calculate total scheduled hours for the week
                var totalWeeklyHours = schedules.Sum(s =>
                    (s.ShiftEnd - s.ShiftStart).TotalHours - (s.BreakDuration?.TotalHours ?? 0));

                return Ok(new
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    EmployeeName = $"{employee?.FirstName} {employee?.LastName}",
                    TotalHours = Math.Round(totalWeeklyHours, 1),
                    DailySchedules = dailySchedules
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving weekly schedules");
                return StatusCode(500, "An error occurred while retrieving weekly schedules.");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Manager,Administrator,2,3")]
        public async Task<IActionResult> UpdateSchedule(int id, UpdateScheduleDto updateDto)
        {
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)
                {
                    return NotFound();
                }

                // Check for overlapping schedules if shift times are being updated
                if (updateDto.ShiftStart.HasValue || updateDto.ShiftEnd.HasValue)
                {
                    var newShiftStart = updateDto.ShiftStart ?? schedule.ShiftStart;
                    var newShiftEnd = updateDto.ShiftEnd ?? schedule.ShiftEnd;

                    bool hasConflict = await _scheduleRepository.HasOverlappingScheduleAsync(
                        schedule.UserId,
                        newShiftStart,
                        newShiftEnd,
                        id);

                    if (hasConflict)
                    {
                        return BadRequest("Employee already has a schedule that overlaps with this time period.");
                    }
                }

                // Update properties if provided
                if (updateDto.ShiftStart.HasValue)
                    schedule.ShiftStart = updateDto.ShiftStart.Value;

                if (updateDto.ShiftEnd.HasValue)
                    schedule.ShiftEnd = updateDto.ShiftEnd.Value;

                if (updateDto.Status.HasValue)
                    schedule.Status = updateDto.Status.Value;

                if (updateDto.Location != null)
                    schedule.Location = updateDto.Location;

                if (updateDto.Notes != null)
                    schedule.Notes = updateDto.Notes;

                if (updateDto.BreakDuration.HasValue)
                    schedule.BreakDuration = updateDto.BreakDuration;

                schedule.ModifiedAt = DateTime.UtcNow;

                await _scheduleRepository.UpdateAsync(schedule);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating schedule {id}");
                return StatusCode(500, "An error occurred while updating the schedule.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Manager,Administrator,2,3")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)
                {
                    return NotFound();
                }

                await _scheduleRepository.DeleteAsync(id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting schedule {id}");
                return StatusCode(500, "An error occurred while deleting the schedule.");
            }
        }

        [HttpGet("team")]
        [Authorize(Roles = "Manager,Administrator,2,3")]
        public async Task<ActionResult<object>> GetTeamSchedules([FromQuery] DateTime weekStart, [FromQuery] List<string> teamMemberIds = null)
        {
            try
            {
                // Get manager ID to fetch their team if no team members specified
                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // TODO: If teamMemberIds is null, fetch the manager's team members
                // This would require a TeamService or similar to get team members

                if (teamMemberIds == null || !teamMemberIds.Any())
                {
                    return BadRequest("Team member IDs are required");
                }

                // Calculate week end
                var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

                // Get schedules for the team
                var teamSchedules = await _scheduleRepository.GetTeamSchedulesAsync(teamMemberIds, weekStart, weekEnd);

                // Group schedules by user
                var groupedSchedules = teamSchedules.GroupBy(s => s.UserId);

                var result = new List<object>();

                foreach (var userGroup in groupedSchedules)
                {
                    var userId = userGroup.Key;
                    var employee = await _userRepository.GetByIdAsync(userId);
                    var employeeName = $"{employee?.FirstName} {employee?.LastName}";

                    // Calculate daily schedules
                    var dailySchedules = new List<object>();

                    for (int i = 0; i < 7; i++)
                    {
                        var currentDate = weekStart.AddDays(i).Date;
                        var scheduleForDay = userGroup.Where(s =>
                            (s.ShiftStart.Date <= currentDate && s.ShiftEnd.Date >= currentDate) ||
                            s.ShiftStart.Date == currentDate ||
                            s.ShiftEnd.Date == currentDate
                        ).ToList();

                        var schedulesForDayDto = scheduleForDay.Select(s => new
                        {
                            Id = s.Id,
                            ShiftStart = s.ShiftStart,
                            ShiftEnd = s.ShiftEnd,
                            Location = s.Location,
                            Status = s.Status.ToString(),
                            Notes = s.Notes,
                            TotalHours = (s.ShiftEnd - s.ShiftStart).TotalHours - (s.BreakDuration?.TotalHours ?? 0)
                        }).ToList();

                        dailySchedules.Add(new
                        {
                            Date = currentDate,
                            DayOfWeek = currentDate.DayOfWeek.ToString(),
                            Schedules = schedulesForDayDto,
                            HasSchedule = schedulesForDayDto.Any()
                        });
                    }

                    // Calculate total hours for this employee
                    var totalEmployeeHours = userGroup.Sum(s =>
                        (s.ShiftEnd - s.ShiftStart).TotalHours - (s.BreakDuration?.TotalHours ?? 0));

                    result.Add(new
                    {
                        UserId = userId,
                        EmployeeName = employeeName,
                        TotalHours = Math.Round(totalEmployeeHours, 1),
                        DailySchedules = dailySchedules
                    });
                }

                return Ok(new
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    TeamSchedules = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving team schedules");
                return StatusCode(500, "An error occurred while retrieving team schedules.");
            }
        }

        [HttpPost("publish/{id}")]
        [Authorize(Roles = "Manager,Administrator,2,3")]
        public async Task<IActionResult> PublishSchedule(int id)
        {
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(id);

                if (schedule == null)
                {
                    return NotFound();
                }

                schedule.Status = ScheduleStatus.Published;
                schedule.ModifiedAt = DateTime.UtcNow;

                await _scheduleRepository.UpdateAsync(schedule);

                // TODO: Send notification to employee about published schedule

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing schedule {id}");
                return StatusCode(500, "An error occurred while publishing the schedule.");
            }
        }

        [HttpPost("bulk-create")]
        [Authorize(Roles = "Manager,Administrator,2,3")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> CreateBulkSchedules(List<CreateScheduleDto> scheduleDtos)
        {
            try
            {
                if (scheduleDtos == null || !scheduleDtos.Any())
                {
                    return BadRequest("No schedules provided");
                }

                var managerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var createdSchedules = new List<Schedule>();

                foreach (var dto in scheduleDtos)
                {
                    // Check for conflicts
                    bool hasConflict = await _scheduleRepository.HasOverlappingScheduleAsync(
                        dto.UserId,
                        dto.ShiftStart,
                        dto.ShiftEnd);

                    if (hasConflict)
                    {
                        continue; // Skip conflicting schedules
                    }

                    var schedule = new Schedule
                    {
                        UserId = dto.UserId,
                        ShiftStart = dto.ShiftStart,
                        ShiftEnd = dto.ShiftEnd,
                        Location = dto.Location,
                        Notes = dto.Notes,
                        BreakDuration = dto.BreakDuration,
                        Status = ScheduleStatus.Pending,
                        CreatedById = managerId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdSchedule = await _scheduleRepository.AddAsync(schedule);
                    createdSchedules.Add(createdSchedule);
                }

                if (!createdSchedules.Any())
                {
                    return BadRequest("Could not create any schedules due to conflicts");
                }

                // Return summary of created schedules
                return Ok(new
                {
                    SchedulesCreated = createdSchedules.Count,
                    SchedulesRequested = scheduleDtos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating bulk schedules");
                return StatusCode(500, "An error occurred while creating schedules.");
            }
        }
    }
}