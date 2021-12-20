using System;

using Azure.Storage.Blobs;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using TimeReport.Application;
using TimeReport.Data;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly TimeReportContext context;

    public ActivitiesController(TimeReportContext context)
    {
        this.context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities(CancellationToken cancellationToken)
    {
        var activities = await context.Activities
            .Include(x => x.Project)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        var dto = activities.Select(activity => new ActivityDto(activity.Id, activity.Name, activity.Description, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description)));
        return Ok(dto);
    }
}

[ApiController]
[Route("api/[controller]")]
public class TimeSheetsController : ControllerBase
{
    private const double WorkingWeekHours = 40;
    private readonly TimeReportContext context;
    private const double WorkingDayHours = 8;

    public TimeSheetsController(TimeReportContext context)
    {
        this.context = context;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TimeSheetDto>> GetTimeSheet([FromRoute] string id, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstAsync();

        var dto = new TimeSheetDto(timeSheet.Id, timeSheet.Year, timeSheet.Week, (TimeSheetStatusDto)timeSheet.Status,
            timeSheet.Entries.OrderBy(e => e.Date).Select(e => new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description)));

        return Ok(dto);
    }

    [HttpGet("{year}/{week}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TimeSheetDto>> GetTimeSheetForWeek([FromRoute] int year, [FromRoute] int week, CancellationToken cancellationToken)
    {
        var timeSheet = await context.TimeSheets
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Year == year && x.Week == week);

        if (timeSheet is null)
        {
            timeSheet = new TimeSheet()
            {
                Id = Guid.NewGuid().ToString(),
                Year = year,
                Week = week
            };

            context.TimeSheets.Add(timeSheet);

            await context.SaveChangesAsync();
        }

        var dto = new TimeSheetDto(timeSheet.Id, timeSheet.Year, timeSheet.Week, (TimeSheetStatusDto)timeSheet.Status,
            timeSheet.Entries.OrderBy(e => e.Date).Select(e => new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description)));

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
            .Include(x => x.Entries)
            .ThenInclude(x => x.Project)
            .Include(x => x.Entries)
            .ThenInclude(x => x.Activity)
            .ThenInclude(x => x.Project)
            .AsSplitQuery()
            .FirstAsync(x => x.Id == timeSheetId);

        if (timeSheet is null)
        {
            return BadRequest();
        }

        var date = DateOnly.FromDateTime(dto.Date);

        var existingEntryWithDate = timeSheet.Entries
            .FirstOrDefault(e => e.Date == date && e.Project.Id == dto.ProjectId && e.Activity.Id == dto.ActivityId);

        if (existingEntryWithDate is not null)
        {
            // Entry for date already exists
        }

        var project = await context.Projects
            .Include(x => x.Activities)
            .FirstOrDefaultAsync(x => x.Id == dto.ProjectId);

        if(project is null)
        {
            // Project not found
        }

        var activity = project!.Activities.FirstOrDefault(x => x.Id == dto.ActivityId);

        if (activity is null)
        {
            // Activity not found
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

        var entry = new Entry
        {
            Id = Guid.NewGuid().ToString(),
            Project = project,
            Activity = activity,
            Date = DateOnly.FromDateTime(dto.Date),
            Hours = dto.Hours,
            Description = dto.Description
        };

        timeSheet.Entries.Add(entry);

        await context.SaveChangesAsync();

        var e = entry;

        var newDto = new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description);

        return Ok(newDto);
    }

    [HttpPut("{timeSheetId}/{entryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EntryDto>> UpdateEntry([FromRoute] string timeSheetId, [FromRoute] string entryId, UpdateEntryDto dto, CancellationToken cancellationToken)
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

        var entry = timeSheet.Entries.FirstOrDefault(e => e.Id == entryId);

        if(entry is null)
        {
            return BadRequest();
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

        var newDto = new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description);

        return Ok(newDto);
    }

    [HttpPut("{timeSheetId}/{entryId}/Details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<EntryDto>> UpdateEntryDetails([FromRoute] string timeSheetId, [FromRoute] string entryId, UpdateEntryDetailsDto dto, CancellationToken cancellationToken)
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

        var entry = timeSheet.Entries.FirstOrDefault(e => e.Id == entryId);

        if (entry is null)
        {
            return BadRequest();
        }

        entry.Description = dto.Description;

        await context.SaveChangesAsync();

        var e = entry;

        var newDto = new EntryDto(e.Id, new ProjectDto(e.Project.Id, e.Project.Name, e.Project.Description), new ActivityDto(e.Activity.Id, e.Activity.Name, e.Activity.Description, new ProjectDto(e.Activity.Project.Id, e.Activity.Project.Name, e.Activity.Project.Description)), e.Date.ToDateTime(TimeOnly.Parse("01:00")), e.Hours, e.Description);

        return Ok(newDto);
    }

    [HttpDelete("{timeSheetId}/{activityId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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

        var activity = await context!.Activities.FirstOrDefaultAsync(x => x.Id == activityId);

        if (activity is null)
        {
            // Activity not found
        }

        var entries = timeSheet.Entries.Where(e => e.Activity.Id == activityId);

        foreach(var entry in entries)
        {
            context.Entries.Remove(entry);
        }

        await context.SaveChangesAsync();

        return Ok();
    }
}

public record class ProjectDto(string Id, string Name, string? Description);

public record class ActivityDto(string Id, string Name, string? Description, ProjectDto Project);

public record class TimeSheetDto(string Id, int Year, int Week, TimeSheetStatusDto Status, IEnumerable<EntryDto> Entries);

[JsonConverter(typeof(StringEnumConverter))]
public enum TimeSheetStatusDto
{
    Open,
    Closed,
    Approved,
    Disapproved
}

public record class EntryDto(string Id, ProjectDto Project, ActivityDto Activity, DateTime Date, double? Hours, string? Description);

public record class CreateEntryDto(string? Id, string? ProjectId, string? ActivityId, DateTime Date, double? Hours, string? Description);

public record class UpdateEntryDto(double? Hours, string? Description);

public record class UpdateEntryDetailsDto(string? Description);

public record class UpdateTimeSheetDto(IEnumerable<UpdateEntryDto2> Entries);

public record class UpdateEntryDto2(string? Id, string? ProjectId, string? ActivityId, DateTime? Date, double? Hours, string? Description);
