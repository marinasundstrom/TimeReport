
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly TimeReportContext context;

    public ProjectsController(TimeReportContext context)
    {
        this.context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects(CancellationToken cancellationToken)
    {
        var projects = await context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        var dto = projects.Select(project => new ProjectDto(project.Id, project.Name, project.Description));
        return Ok(dto);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDto>> GetProject(string id)
    {
        var project = await context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        var dto = new ProjectDto(project.Id, project.Name, project.Description);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto createProjectDto)
    {
        var project = new Project
        {
            Id = Guid.NewGuid().ToString(),
            Name = createProjectDto.Name,
            Description = createProjectDto.Description
        };

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        var dto = new ProjectDto(project.Id, project.Name, project.Description);
        return Ok(dto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDto>> UpdateProject(string id, UpdateProjectDto updateProjectDto)
    {
        var project = await context.Projects
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        project.Name = updateProjectDto.Name;
        project.Description = updateProjectDto.Description;

        await context.SaveChangesAsync();

        var dto = new ProjectDto(project.Id, project.Name, project.Description);
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteProject(string id)
    {
        var project = await context.Projects
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        context.Projects.Remove(project);

        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("Statistics")]
    public async Task<ActionResult<Data>> GetStatistics()
    {
        var projects = await context.Projects
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        List<DateTime> months = new();

        const int monthSpan = 5;

        DateTime dt = DateTime.Now.Date.AddMonths(-monthSpan);

        for (int i = 0; i <= monthSpan; i++)
        {
            months.Add(dt);

            dt = dt.AddMonths(1);
        }

        List<Series> series = new();

        var firstMonth = DateOnly.FromDateTime(DateTime.Now.Date.AddMonths(-monthSpan));

        foreach (var project in projects)
        {
            List<decimal> values = new ();

            foreach(var month in months)
            {
                var value = project.Activities.SelectMany(a => a.Entries)
                    .Where(e => e.Date > firstMonth)
                    .Where(e => e.Date.Year == month.Year && e.Date.Month == month.Month)
                    .Sum(x => x.Hours.GetValueOrDefault());

                values.Add((decimal)value);
            }

            series.Add(new Series(project.Name, values));
        }

        var dto = new Data(
            months.Select(d => d.ToString("MMM yy")).ToArray(),
            series);

        return Ok(dto);
    }

    [HttpGet("{projectId}/Statistics")]
    public async Task<ActionResult<Data>> GetProjectStatistics(string projectId)
    {
        var project = await context.Projects
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project is null)
        {
            return NotFound();
        }

        List<DateTime> months = new();

        const int monthSpan = 5;

        DateTime dt = DateTime.Now.Date.AddMonths(-monthSpan);

        for (int i = 0; i <= monthSpan; i++)
        {
            months.Add(dt);

            dt = dt.AddMonths(1);
        }

        List<Series> series = new();

        var firstMonth = DateOnly.FromDateTime(DateTime.Now.Date.AddMonths(-monthSpan));

        foreach (var activity in project.Activities)
        {
            List<decimal> values = new();

            foreach (var month in months)
            {
                var value = activity.Entries
                    .Where(e => e.Date > firstMonth)
                    .Where(e => e.Date.Year == month.Year && e.Date.Month == month.Month)
                    .Sum(x => x.Hours.GetValueOrDefault());

                values.Add((decimal)value);
            }

            series.Add(new Series(activity.Name, values));
        }

        var dto = new Data(
            months.Select(d => d.ToString("MMM yy")).ToArray(),
            series);

        return Ok(dto);
    }
}

public record class CreateProjectDto(string Name, string? Description);

public record class UpdateProjectDto(string Name, string? Description);