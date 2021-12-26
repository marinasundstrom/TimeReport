
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task<ActionResult<ItemsResult<ActivityDto>>> GetActivities(int page = 0, int pageSize = 10, string? projectId = null, string? searchString = null)
    {
        var query = context.Activities
            .Include(x => x.Project)
            .OrderBy(p => p.Created)
            .AsNoTracking()
            .AsSplitQuery();

        if(projectId is not null)
        {
            query = query.Where(activity => activity.Project.Id == projectId);
        }

        if(searchString is not null)
        {
            query = query.Where(activity => activity.Name.ToLower().Contains(searchString.ToLower()));
        }

        var totalItems = await query.CountAsync();

        var activities = await query
            .Skip(pageSize * page)
            .Take(pageSize)   
            .ToListAsync();

        var dtos = activities.Select(activity => new ActivityDto(activity.Id, activity.Name, activity.Description, activity.HourlyRate, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description)));
        
        return Ok(new ItemsResult<ActivityDto>(dtos, totalItems));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> GetActivity(string id)
    {
        var activity = await context.Activities
            .Include(x => x.Project)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (activity is null)
        {
            return NotFound();
        }

        var dto = new ActivityDto(activity.Id, activity.Name, activity.Description, activity.HourlyRate, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description));
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> CreateActivity(string projectId, CreateActivityDto createActivityDto)
    {
        var project = await context.Projects
           .AsSplitQuery()
           .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project is null)
        {
            return NotFound();
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid().ToString(),
            Name = createActivityDto.Name,
            Description = createActivityDto.Description,
            Project = project,
            HourlyRate = createActivityDto.HourlyRate
        };

        context.Activities.Add(activity);

        await context.SaveChangesAsync();

        var dto = new ActivityDto(activity.Id, activity.Name, activity.Description, activity.HourlyRate, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description));
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> UpdateActivity(string id, UpdateActivityDto updateActivityDto)
    {
        var activity = await context.Activities
            .Include(x => x.Project)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (activity is null)
        {
            return NotFound();
        }

        activity.Name = updateActivityDto.Name;
        activity.Description = updateActivityDto.Description;
        activity.HourlyRate = updateActivityDto.HourlyRate;

        await context.SaveChangesAsync();

        var dto = new ActivityDto(activity.Id, activity.Name, activity.Description, activity.HourlyRate, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description));
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteActivity(string id)
    {
        var activity = await context.Activities
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (activity is null)
        {
            return NotFound();
        }

        context.Activities.Remove(activity);

        await context.SaveChangesAsync();

        return Ok();
    }


    [HttpGet("{id}/Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary(string id)
    {
        var activity = await context.Activities
            .Include(x => x.Entries)
            .ThenInclude(x => x.User)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (activity is null)
        {
            return NotFound();
        }

        var totalHours = activity.Entries
            .Sum(p => p.Hours.GetValueOrDefault());

        var totalUsers = activity.Entries
            .Select(p => p.User)
            .DistinctBy(p => p.Id)
            .Count();

        return new StatisticsSummary(new StatisticsSummaryEntry[]
        {
            new ("Participants", totalUsers),
            new ("Hours", totalHours)
        });
    }
}

public record class CreateActivityDto(string Name, string? Description, decimal? HourlyRate);

public record class UpdateActivityDto(string Name, string? Description, decimal? HourlyRate);