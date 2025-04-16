using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TimeCardSystem.Core.Interfaces;
using TimeCardSystem.Core.Dtos;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeEntryController : ControllerBase
    {
        private readonly ITimeEntryRepository _timeEntryRepository;

        public TimeEntryController(ITimeEntryRepository timeEntryRepository)
        {
            _timeEntryRepository = timeEntryRepository;
        }

        [HttpPost]
        public async Task<ActionResult<TimeEntryDto>> CreateTimeEntry(TimeEntryDto timeEntryDto)
        {
            var timeEntry = new TimeEntry
            {
                UserId = timeEntryDto.UserId,
                ClockIn = timeEntryDto.ClockIn,
                ClockOut = timeEntryDto.ClockOut,
                BreakDuration = timeEntryDto.BreakDuration,
                LunchClockIn = timeEntryDto.LunchClockIn,
                LunchClockOut = timeEntryDto.LunchClockOut,
                Status = timeEntryDto.Status
            };

            var createdTimeEntry = await _timeEntryRepository.AddAsync(timeEntry);
            return CreatedAtAction(nameof(GetTimeEntry), new { id = createdTimeEntry.Id }, createdTimeEntry);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimeEntryDto>> GetTimeEntry(int id)
        {
            var timeEntry = await _timeEntryRepository.GetByIdAsync(id);

            if (timeEntry == null)
            {
                return NotFound();
            }

            return Ok(new TimeEntryDto
            {
                Id = timeEntry.Id,
                UserId = timeEntry.UserId,
                ClockIn = timeEntry.ClockIn,
                ClockOut = timeEntry.ClockOut,
                BreakDuration = timeEntry.BreakDuration,
                LunchClockIn = timeEntry.LunchClockIn,
                LunchClockOut = timeEntry.LunchClockOut,
                Status = timeEntry.Status,
                TotalWorkTime = timeEntry.TotalWorkTime
            });
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<TimeEntryDto>>> GetUserTimeEntries(string userId)
        {
            var timeEntries = await _timeEntryRepository.GetByUserIdAsync(userId);

            return Ok(timeEntries.Select(te => new TimeEntryDto
            {
                Id = te.Id,
                UserId = te.UserId,
                ClockIn = te.ClockIn,
                ClockOut = te.ClockOut,
                BreakDuration = te.BreakDuration,
                LunchClockIn = te.LunchClockIn,
                LunchClockOut = te.LunchClockOut,
                Status = te.Status,
                TotalWorkTime = te.TotalWorkTime
            }));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTimeEntry(int id, TimeEntryDto timeEntryDto)
        {
            var existingTimeEntry = await _timeEntryRepository.GetByIdAsync(id);

            if (existingTimeEntry == null)
            {
                return NotFound();
            }

            existingTimeEntry.ClockOut = timeEntryDto.ClockOut;
            existingTimeEntry.BreakDuration = timeEntryDto.BreakDuration;
            existingTimeEntry.LunchClockIn = timeEntryDto.LunchClockIn;
            existingTimeEntry.LunchClockOut = timeEntryDto.LunchClockOut;
            existingTimeEntry.Status = timeEntryDto.Status;

            await _timeEntryRepository.UpdateAsync(existingTimeEntry);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeEntry(int id)
        {
            await _timeEntryRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("weekly")]
        [Authorize]
        public async Task<ActionResult<object>> GetWeeklyTimeEntries([FromQuery] DateTime weekStart, [FromQuery] string userId = null)
        {
            try
            {
                // If no userId provided, use the current user's ID (for authenticated users)
                if (string.IsNullOrEmpty(userId) && User.Identity.IsAuthenticated)
                {
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID is required");
                }

                // Calculate week end (7 days from start)
                var weekEnd = weekStart.AddDays(7).AddSeconds(-1);

                // Get time entries for the week
                var entries = await _timeEntryRepository.GetEntriesByDateRangeAsync(userId, weekStart, weekEnd);

                // Create a list for all days in the week
                var dailyEntries = new List<object>();

                for (int i = 0; i < 7; i++)
                {
                    var currentDate = weekStart.AddDays(i).Date;
                    var entriesForDay = entries.Where(e => e.ClockIn.Date == currentDate).ToList();

                    var totalHoursForDay = entriesForDay.Sum(e =>
                        e.TotalWorkTime.HasValue ? e.TotalWorkTime.Value.TotalHours : 0);

                    dailyEntries.Add(new
                    {
                        Date = currentDate,
                        DayOfWeek = currentDate.DayOfWeek.ToString(),
                        Entries = entriesForDay.Select(e => new
                        {
                            Id = e.Id,
                            ClockIn = e.ClockIn,
                            ClockOut = e.ClockOut,
                            TotalHours = e.TotalWorkTime.HasValue ? Math.Round(e.TotalWorkTime.Value.TotalHours, 1) : 0,
                            Status = e.Status.ToString()
                        }).ToList(),
                        TotalHours = Math.Round(totalHoursForDay, 1)
                    });
                }

                // Calculate total weekly hours
                var totalWeeklyHours = dailyEntries.Sum(d => (double)((dynamic)d).TotalHours);

                return Ok(new
                {
                    WeekStart = weekStart,
                    WeekEnd = weekEnd,
                    TotalHours = Math.Round(totalWeeklyHours, 1),
                    DailyEntries = dailyEntries
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
    }
