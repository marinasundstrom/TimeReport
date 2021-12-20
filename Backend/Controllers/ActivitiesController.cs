
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
    public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities(string? projectId = null)
    {
        var query = context.Activities
            .Include(x => x.Project)
            .AsNoTracking()
            .AsSplitQuery();

        if(projectId is not null)
        {
            query = query.Where(activity => activity.Project.Id == projectId);
        }

        var activities = await query.ToListAsync();

        var dto = activities.Select(activity => new ActivityDto(activity.Id, activity.Name, activity.Description, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description)));
        return Ok(dto);
    }
}