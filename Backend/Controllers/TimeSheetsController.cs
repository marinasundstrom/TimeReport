using System;

using Azure.Storage.Blobs;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Domain.Entities;
using TimeReport.Infrastructure;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimeSheetsController : ControllerBase
{
    private const double WorkingWeekHours = 40;
    private readonly ITimeReportContext context;
    private const double WorkingDayHours = 8;

    public TimeSheetsController(ITimeReportContext context)
    {
        this.context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<TimeSheetDto>>> GetTimeSheets(int page = 0, int pageSize = 10, string? projectId = null, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null)
    {
        var query = context.TimeSheets
            .Include(x => x.User)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .ThenInclude(x => x.MonthGroup)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Project)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Week)
            .AsNoTracking()
            .AsSplitQuery();

        if (projectId is not null)
        {
            query = query.Where(timeSheet => timeSheet.Activities.Any(x => x.Project.Id == projectId));
        }

        if (searchString is not null)
        {
            query = query.Where(timeSheet => timeSheet.Id.ToLower().Contains(searchString.ToLower()));
        }

        var totalItems = await query.CountAsync();

        if (sortBy is not null)
        {
            query = query.OrderBy(sortBy, sortDirection == SortDirection.Desc ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
        }

        var timeSheets = await query
            .Skip(pageSize * page)
            .Take(pageSize)
            .ToListAsync();

        var monthInfo = await context.MonthEntryGroups
            .Where(x => x.Status == EntryStatus.Locked)
            .ToArrayAsync();

        var results = new ItemsResult<TimeSheetDto>(
            timeSheets.Select(timeSheet =>
            {
                var activities = timeSheet.Activities
                    .OrderBy(e => e.Created)
                    .Select(e => new TimeSheetActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description),
                        e.Entries.OrderBy(e => e.Date).Select(e => new TimeSheetEntryDto(e.Id, e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description, (EntryStatusDto)e.MonthGroup.Status))));

                var m = monthInfo
                        .Where(x => x.User.Id == timeSheet.User.Id)
                        .Where(x => x.Month == timeSheet.From.Month || x.Month == timeSheet.To.Month);

                return new TimeSheetDto(timeSheet.Id, timeSheet.Year, timeSheet.Week, timeSheet.From, timeSheet.To, (TimeSheetStatusDto)timeSheet.Status, new UserDto(timeSheet.User.Id, timeSheet.User.FirstName, timeSheet.User.LastName, timeSheet.User.DisplayName, timeSheet.User.SSN, timeSheet.User.Email, timeSheet.User.Created, timeSheet.User.Deleted), activities,
                    monthInfo.Select(x => new MonthInfoDto(x.Month, x.Status == EntryStatus.Locked)));
            }),
            totalItems);

        return Ok(results);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TimeSheetDto>> GetTimeSheet([FromRoute] string id, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.User)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .ThenInclude(x => x.MonthGroup)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Project)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstAsync();

        var activities = timeSheet.Activities
            .OrderBy(e => e.Created)
            .Select(e => new TimeSheetActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description),
                e.Entries.OrderBy(e => e.Date).Select(e => new TimeSheetEntryDto(e.Id, e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description, (EntryStatusDto)e.MonthGroup.Status))));

        var monthInfo = await context.MonthEntryGroups
            .Where(x => x.User.Id == timeSheet.User.Id)
            .Where(x => x.Month == timeSheet.From.Month || x.Month == timeSheet.To.Month)
            .ToArrayAsync();

        var dto = new TimeSheetDto(timeSheet.Id, timeSheet.Year, timeSheet.Week, timeSheet.From, timeSheet.To, (TimeSheetStatusDto)timeSheet.Status, new UserDto(timeSheet.User.Id, timeSheet.User.FirstName, timeSheet.User.LastName, timeSheet.User.DisplayName, timeSheet.User.SSN, timeSheet.User.Email, timeSheet.User.Created, timeSheet.User.Deleted),
            activities, monthInfo.Select(x => new MonthInfoDto(x.Month, x.Status == EntryStatus.Locked)));

        return Ok(dto);
    }

    [HttpGet("{year}/{week}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TimeSheetDto>> GetTimeSheetForWeek([FromRoute] int year, [FromRoute] int week, [FromQuery] string? userId, CancellationToken cancellationToken)
    {
        var query = context.TimeSheets
            .Include(x => x.User)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .ThenInclude(x => x.MonthGroup)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Project)
            .Include(x => x.Activities)
            .AsSplitQuery();

        if (userId is not null)
        {
            query = query.Where(x => x.User.Id == userId);
        }

        var timeSheet = await query.FirstOrDefaultAsync(x => x.Year == year && x.Week == week);

        if (timeSheet is null)
        {
            User? user = null;

            if (userId is not null)
            {
                user = await context.Users.FirstAsync(x => x.Id == userId);
            }
            else
            {
                user = await context.Users.FirstOrDefaultAsync();
            }

            var startDate = System.Globalization.ISOWeek.ToDateTime(year, week, DayOfWeek.Monday);

            timeSheet = new TimeSheet()
            {
                Id = Guid.NewGuid().ToString(),
                Year = year,
                Week = week,
                From = startDate,
                To = startDate.AddDays(6),
                User = user
            };

            context.TimeSheets.Add(timeSheet);

            await context.SaveChangesAsync();
        }

        var activities = timeSheet.Activities
            .OrderBy(e => e.Created)
            .Select(e => new TimeSheetActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description),
                e.Entries.OrderBy(e => e.Date).Select(e => new TimeSheetEntryDto(e.Id, e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description, (EntryStatusDto)e.MonthGroup.Status))))
            .ToArray();

        var monthInfo = await context.MonthEntryGroups
            .Where(x => x.User.Id == timeSheet.User.Id)
            .Where(x => x.Month == timeSheet.From.Month || x.Month == timeSheet.To.Month)
            .ToArrayAsync();

        var dto = new TimeSheetDto(timeSheet.Id, timeSheet.Year, timeSheet.Week, timeSheet.From, timeSheet.To, (TimeSheetStatusDto)timeSheet.Status, new UserDto(timeSheet.User.Id, timeSheet.User.FirstName, timeSheet.User.LastName, timeSheet.User.DisplayName, timeSheet.User.SSN, timeSheet.User.Email, timeSheet.User.Created, timeSheet.User.Deleted),
            activities, monthInfo.Select(x => new MonthInfoDto(x.Month, x.Status == EntryStatus.Locked)));

        return Ok(dto);
    }

    /*

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> UpdateTimeSheet([FromRoute] string id, UpdateTimeSheetDto dto, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync();

        foreach (var entryDto in dto.Entries)
        {
            Entry? existingEntry = null;
            bool newEntry = false;

            if(entryDto.Id is not null)
            {
                existingEntry = timeSheet.Entries.FirstOrDefault(e => e.Id == entryDto.Id);

                if (existingEntry is null)
                {
                    // Entry with Id does not exist
                    // newEntry = true;
                }
            }

            if (entryDto.ProjectId is null)
            {
                // ProjectId is null
            }
            else
            {
                if(existingEntry is not null)
                {
                    if(existingEntry?.Project.Id != entryDto.ProjectId)
                    {

                    }
                }
            }

            if (entryDto.ActivityId is null)
            {
                // ActivityId is null
            }
            else
            {
                if (existingEntry is not null)
                {
                    if (existingEntry?.Activity.Project.Id != entryDto.ProjectId)
                    {
                        // Acticity is not in project
                    }

                    if(existingEntry.Activity.Id != entryDto.ActivityId)
                    {

                    }
                }
            }

            if (entryDto.Date is null)
            {
                // Date should not be null
            }

            var date = DateOnly.FromDateTime(entryDto.Date.GetValueOrDefault());

            var existingEntryWithDate = timeSheet.Entries.First(e => e.Date == date);

            if (existingEntryWithDate is not null)
            {
                // Entry for date already exists
            }
        }

        foreach (var entryDto in dto.Entries)
        {
            Entry? entry = null;
            bool newEntry = false;

            if (entryDto.Id is null)
            {
                newEntry = true;

                entry = new Entry()
                {
                    Id = Guid.NewGuid().ToString()
                };
                timeSheet.Entries.Add(entry);
            }
            else
            {
                entry = timeSheet.Entries.FirstOrDefault(e => e.Id == entryDto.Id);

                newEntry = false;
            }

            entry.Hours = entryDto.Hours;
            entry.Description = entryDto.Description;
        }

        await context.SaveChangesAsync();

        return Ok(dto);
    }

    */

    [HttpPost("{timeSheetId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntryDto>> CreateEntry([FromRoute] string timeSheetId, CreateEntryDto dto, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.User)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return NotFound();
        }

        if (timeSheet.Status != TimeSheetStatus.Open)
        {
            return Problem(
                          title: "Timesheet is closed",
                          detail: $"Updating entries of a Timesheet when not in Open state is not allowed.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var group = await context.MonthEntryGroups
            .FirstOrDefaultAsync(meg =>
                meg.User.Id == timeSheet.User.Id
                && meg.Year == dto.Date.Year
                && meg.Month == dto.Date.Month);

        if (group is null)
        {
            group = new MonthEntryGroup
            {
                Id = Guid.NewGuid().ToString(),
                User = timeSheet.User,
                Year = dto.Date.Year,
                Month = dto.Date.Month,
                Status = EntryStatus.Unlocked
            };

            context.MonthEntryGroups.Add(group);
        }
        else
        {
            if (group.Status == EntryStatus.Locked)
            {
                return Problem(
                          title: "Month is locked",
                          detail: $"Updating entries of a month that has been locked is not allowed.",
                          statusCode: StatusCodes.Status403Forbidden);
            }
        }

        var date = DateOnly.FromDateTime(dto.Date);

        var existingEntryWithDate = timeSheet.Entries
            .FirstOrDefault(e => e.Date == date && e.Project.Id == dto.ProjectId && e.Activity.Id == dto.ActivityId);

        if (existingEntryWithDate is not null)
        {
            return Problem(
                          title: "Entry already exists",
                          detail: $"Entry for this activity and date does already exist.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var project = await context.Projects
            .Include(x => x.Activities)
            .FirstOrDefaultAsync(x => x.Id == dto.ProjectId);

        if (project is null)
        {
            return Problem(
                           title: "Project not found",
                           detail: $"No project with Id {dto.ProjectId} was found.",
                           statusCode: StatusCodes.Status403Forbidden);
        }

        var activity = project!.Activities.FirstOrDefault(x => x.Id == dto.ActivityId);

        if (activity is null)
        {
            return Problem(
                          title: "Activity not found",
                          detail: $"No activity with Id {dto.ActivityId} was found.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var dateOnly = DateOnly.FromDateTime(dto.Date);

        double totalHoursDay = timeSheet.Entries.Where(e => e.Date == dateOnly).Sum(e => e.Hours.GetValueOrDefault())
            + dto.Hours.GetValueOrDefault();

        if (totalHoursDay > WorkingDayHours)
        {
            return Problem(
                title: "Exceeds permitted daily working hours",
                detail: $"Reported daily time exceeds {WorkingDayHours} hours.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        double totalHoursWeek = timeSheet.Entries.Sum(x => x.Hours.GetValueOrDefault())
            + dto.Hours.GetValueOrDefault();

        if (totalHoursWeek > WorkingWeekHours)
        {
            return Problem(
                title: "Exceeds permitted weekly working hours",
                detail: $"Reported weekly time exceeds {WorkingWeekHours} hours.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var timeSheetActivity = await context.TimeSheetActivities
            .FirstOrDefaultAsync(x => x.TimeSheet.Id == timeSheet.Id && x.Activity.Id == activity.Id);

        if (timeSheetActivity is null)
        {
            timeSheetActivity = new TimeSheetActivity
            {
                Id = Guid.NewGuid().ToString(),
                TimeSheet = timeSheet,
                Project = project,
                Activity = activity
            };

            context.TimeSheetActivities.Add(timeSheetActivity);
        }

        var entry = new Entry
        {
            Id = Guid.NewGuid().ToString(),
            User = timeSheet.User,
            Project = project,
            Activity = activity,
            TimeSheetActivity = timeSheetActivity,
            Date = DateOnly.FromDateTime(dto.Date),
            Hours = dto.Hours,
            Description = dto.Description
        };

        timeSheet.Entries.Add(entry);

        entry.MonthGroup = group;

        group.Entries.Add(entry);

        await context.SaveChangesAsync();

        var e = entry;

        var newDto = new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, e.Activity.HourlyRate, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description, (EntryStatusDto)e.MonthGroup.Status);

        return Ok(newDto);
    }

    [HttpPut("{timeSheetId}/{entryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntryDto>> UpdateEntry([FromRoute] string timeSheetId, [FromRoute] string entryId, UpdateEntryDto dto, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.MonthGroup)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return NotFound();
        }

        if (timeSheet.Status != TimeSheetStatus.Open)
        {
            return Problem(
                          title: "Timesheet is closed",
                          detail: $"Updating entries of a Timesheet when not in Open state is not allowed.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var entry = timeSheet.Entries.FirstOrDefault(e => e.Id == entryId);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.MonthGroup.Status == EntryStatus.Locked)
        {
            return Problem(
                      title: "Month is locked",
                      detail: $"Updating entries of a month that has been locked is not allowed.",
                      statusCode: StatusCodes.Status403Forbidden);
        }

        entry.Hours = dto.Hours;
        entry.Description = dto.Description;

        double totalHoursDay = timeSheet.Entries.Where(e => e.Date == entry.Date).Sum(e => e.Hours.GetValueOrDefault());
        if (totalHoursDay > WorkingDayHours)
        {
            return Problem(
                title: "Exceeds permitted daily working hours",
                detail: $"Reported daily time exceeds {WorkingDayHours} hours.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        double totalHoursWeek = timeSheet.Entries.Sum(x => x.Hours.GetValueOrDefault());
        if (totalHoursWeek > WorkingWeekHours)
        {
            return Problem(
                title: "Exceeds permitted weekly working hours",
                detail: $"Reported weekly time exceeds {WorkingWeekHours} hours.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        await context.SaveChangesAsync();

        var e = entry;

        var newDto = new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, e.Activity.HourlyRate, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description, (EntryStatusDto)e.MonthGroup.Status);

        return Ok(newDto);
    }

    [HttpPut("{timeSheetId}/{entryId}/Details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntryDto>> UpdateEntryDetails([FromRoute] string timeSheetId, [FromRoute] string entryId, UpdateEntryDetailsDto dto, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.MonthGroup)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return BadRequest();
        }

        if (timeSheet.Status != TimeSheetStatus.Open)
        {
            return Problem(
                          title: "Timesheet is closed",
                          detail: $"Updating entries of a Timesheet when not in Open state is not allowed.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var entry = timeSheet.Entries.FirstOrDefault(e => e.Id == entryId);

        if (entry is null)
        {
            return BadRequest();
        }

        if (entry.MonthGroup.Status == EntryStatus.Locked)
        {
            return Problem(
                      title: "Month is locked",
                      detail: $"Updating entries of a month that has been locked is not allowed.",
                      statusCode: StatusCodes.Status403Forbidden);
        }

        entry.Description = dto.Description;

        await context.SaveChangesAsync();

        var e = entry;

        var newDto = new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, e.Activity.HourlyRate, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description, (EntryStatusDto)e.MonthGroup.Status);

        return Ok(newDto);
    }

    [HttpDelete("{timeSheetId}/{activityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteActvityEntries([FromRoute] string timeSheetId, [FromRoute] string activityId, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return BadRequest();
        }

        if (timeSheet.Status != TimeSheetStatus.Open)
        {
            return Problem(
                          title: "Timesheet is closed",
                          detail: $"Updating entries of a Timesheet when not in Open state is not allowed.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var activity = await context!.Activities.FirstOrDefaultAsync(x => x.Id == activityId);

        if (activity is null)
        {
            return Problem(
                          title: "Activity not found",
                          detail: $"No activity with Id {activityId} was found.",
                          statusCode: StatusCodes.Status403Forbidden);
        }

        var entries = timeSheet.Entries.Where(e => e.Activity.Id == activityId);

        foreach (var entry in entries.Where(e => e.Status == EntryStatus.Unlocked))
        {
            context.Entries.Remove(entry);
        }


        if (entries.All(e => e.Status == EntryStatus.Unlocked))
        {
            var timeSheetActivity = await context.TimeSheetActivities
                .FirstOrDefaultAsync(x => x.TimeSheet.Id == timeSheet.Id && x.Activity.Id == activity.Id);

            if (timeSheetActivity is not null)
            {
                context.TimeSheetActivities.Remove(timeSheetActivity);
            }
        }

        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{timeSheetId}/CloseWeek")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CloseWeek([FromRoute] string timeSheetId)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return BadRequest();
        }

        timeSheet.Status = TimeSheetStatus.Closed;
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPut("{timeSheetId}/Status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> UpdateTimeSheetStatus([FromRoute] string timeSheetId, int statusCode)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return BadRequest();
        }

        timeSheet.Status = (TimeSheetStatus)statusCode;
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{timeSheetId}/LockMonth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> LockMonth([FromRoute] string timeSheetId)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .Include(x => x.User)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return BadRequest();
        }

        var firstWeekDay = timeSheet.From;
        var lastWeekDay = timeSheet.To;

        int month = firstWeekDay.Month;

        DateTime firstDate;
        DateTime lastDate;

        if (firstWeekDay.Month == lastWeekDay.Month)
        {
            int daysInMonth = DateTime.DaysInMonth(firstWeekDay.Month, month);

            if (lastWeekDay.Month == daysInMonth)
            {
                firstDate = new DateTime(firstWeekDay.Year, firstWeekDay.Month, 1);
                lastDate = lastWeekDay;
            }
            else
            {
                return Problem(
                          title: "Failed to lock month",
                          detail: $"Unable to lock month in this timesheet.",
                          statusCode: StatusCodes.Status403Forbidden);
            }
        }
        else
        {
            firstDate = new DateTime(firstWeekDay.Year, firstWeekDay.Month, 1);

            int daysInMonth = DateTime.DaysInMonth(firstWeekDay.Month, month);
            lastDate = new DateTime(firstWeekDay.Year, firstWeekDay.Month, daysInMonth);
        }

        var userId = timeSheet.User.Id;

        var group = await context.MonthEntryGroups
           .Include(meg => meg.Entries)
           .FirstOrDefaultAsync(meg =>
               meg.User.Id == userId
               && meg.Year == lastDate.Date.Year
               && meg.Month == lastDate.Date.Month);

        if (group is not null)
        {
            if (group.Status == EntryStatus.Locked)
            {
                return Problem(
                          title: "Unable to lock month",
                          detail: $"Month is already locked.",
                          statusCode: StatusCodes.Status403Forbidden);
            }

            group.Status = EntryStatus.Locked;

            foreach (var entry in group.Entries)
            {
                entry.Status = EntryStatus.Locked;
            }

            await context.SaveChangesAsync();
        }

        return Ok();
    }
}