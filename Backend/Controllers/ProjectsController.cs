
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
    public async Task<ActionResult<ItemsResult<ProjectDto>>> GetProjects(string? userId = null, int page = 0, int pageSize = 10, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null)
    {
        var query = context.Projects
            .Include(p => p.Memberships)
            .OrderBy(p => p.Created)
            .Skip(pageSize * page)
            .Take(pageSize)
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();

        if(userId is not null)
        {
            query = query.Where(p => p.Memberships.Any(x => x.User.Id ==userId));
        }

        if (searchString is not null)
        {
            query = query.Where(p => p.Name.ToLower().Contains(searchString.ToLower()));
        }

        var totalItems = await query.CountAsync();

        if (sortBy is not null)
        {
            query = query.OrderBy(sortBy, sortDirection == SortDirection.Desc ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
        }

        var projects = await query.ToArrayAsync();

        var dtos = projects.Select(project => new ProjectDto(project.Id, project.Name, project.Description));
        
        return Ok(new ItemsResult<ProjectDto>(dtos, totalItems));
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

    [HttpGet("Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary()
    {
        var entries = await context.Entries
            .CountAsync();

        var totalProjects = await context.Projects
           .CountAsync();

        var totalUsers = await context.Users
            .CountAsync();

        var totalHours = await context.Entries
            .SumAsync(p => p.Hours.GetValueOrDefault());

        var revenue = await context.Entries
            .Where(e => e.Activity.HourlyRate.GetValueOrDefault() > 0)
            .SumAsync(e => e.Activity.HourlyRate.GetValueOrDefault() * (decimal)e.Hours.GetValueOrDefault());

        var expenses = await context.Entries
             .Where(e => e.Activity.HourlyRate.GetValueOrDefault() < 0)
             .SumAsync(e => e.Activity.HourlyRate.GetValueOrDefault() * (decimal)e.Hours.GetValueOrDefault());

        return new StatisticsSummary(new StatisticsSummaryEntry[]
        {
            new ("Projects", totalProjects),
            new ("Users", totalUsers),
            new ("Hours", totalHours),
            new ("Revenue", null, revenue,  unit: "currency"),
            new ("Expenses", null, expenses, unit: "currency")
        });
    }

    [HttpGet("Statistics")]
    public async Task<ActionResult<Data>> GetStatistics(DateTime? from = null, DateTime? to = null)
    {
        var projects = await context.Projects
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        List<DateTime> months = new();

        const int monthSpan = 5;

        DateTime lastDate = to?.Date ?? DateTime.Now.Date;
        DateTime firstDate = from?.Date ?? lastDate.AddMonths(-monthSpan)!;

        for (DateTime dt = firstDate; dt <= lastDate; dt = dt.AddMonths(1))
        {
            months.Add(dt);
        }

        List<Series> series = new();

        var firstMonth = DateOnly.FromDateTime(firstDate);
        var lastMonth = DateOnly.FromDateTime(lastDate);

        foreach (var project in projects)
        {
            List<decimal> values = new();

            foreach (var month in months)
            {
                var value = project.Activities.SelectMany(a => a.Entries)
                    .Where(e => e.Date.Month > firstMonth.Month)
                    .Where(e => e.Date.Month <= lastMonth.Month)
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

    [HttpGet("{projectId}/Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary(string projectId)
    {
        var project = await context.Projects
            .Include(p => p.Entries)
            .ThenInclude(x => x.User)
            .Include(p => p.Entries)
            .ThenInclude(x => x.Activity)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project is null)
        {
            return NotFound();
        }

        var totalHours = project.Entries
            .Sum(e => e.Hours.GetValueOrDefault());

        var revenue = project.Entries
            .Where(e => e.Activity.HourlyRate.GetValueOrDefault() > 0)
            .Sum(e => e.Activity.HourlyRate.GetValueOrDefault() * (decimal)e.Hours.GetValueOrDefault());

        var expenses = project.Entries
             .Where(e => e.Activity.HourlyRate.GetValueOrDefault() < 0)
             .Sum(e => e.Activity.HourlyRate.GetValueOrDefault() * (decimal)e.Hours.GetValueOrDefault());

        var totalUsers = project.Entries
            .Select(e => e.User)
            .DistinctBy(e => e.Id)
            .Count();

        return new StatisticsSummary(new StatisticsSummaryEntry[]
        {
            new ("Participants", totalUsers),
            new ("Hours", totalHours),
            new ("Revenue", null, revenue, unit: "currency"),
            new ("Expenses", null, expenses, unit: "currency")
        });
    }

    [HttpGet("{projectId}/Statistics")]
    public async Task<ActionResult<Data>> GetProjectStatistics(string projectId, DateTime? from = null, DateTime? to = null)
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

        DateTime lastDate = to?.Date ?? DateTime.Now.Date;
        DateTime firstDate = from?.Date ?? lastDate.AddMonths(-monthSpan)!;

        for (DateTime dt = firstDate; dt <= lastDate; dt = dt.AddMonths(1))
        {
            months.Add(dt);
        }

        List<Series> series = new();

        var firstMonth = DateOnly.FromDateTime(firstDate);
        var lastMonth = DateOnly.FromDateTime(lastDate);

        foreach (var activity in project.Activities)
        {
            List<decimal> values = new();

            foreach (var month in months)
            {
                var value = activity.Entries
                    .Where(e => e.Date.Month > firstMonth.Month)
                    .Where(e => e.Date.Month <= lastMonth.Month)
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

    [HttpGet("{id}/Memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ProjectMembershipDto>>> GetProjectMemberships(string id, int page = 0, int pageSize = 10, string? sortBy = null, SortDirection? sortDirection = null)
    {
        var project = await context.Projects
            .OrderBy(p => p.Created)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        var query = context.ProjectMemberships
                .OrderBy(p => p.Created)
                .Where(m => m.Project.Id == project.Id);

        var totalItems = await query.CountAsync();

        if (sortBy is not null)
        {
            query = query.OrderBy(sortBy, sortDirection == SortDirection.Desc ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
        }

        var memberships = await query
                .Include(m => m.User)
                .Skip(pageSize * page)
                .Take(pageSize)
                .ToArrayAsync();

        var dtos = memberships
            .Select(m => new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Email, m.User.Created, m.User.Deleted),
            m.From, m.Thru));

        return Ok(new ItemsResult<ProjectMembershipDto>(dtos, totalItems));
    }

    [HttpGet("{id}/Memberships/{membershipId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectMembershipDto>> GetProjectMembership(string id, string membershipId)
    {
        var project = await context.Projects
            .Include(p => p.Memberships)
            .Include(p => p.Memberships)
            .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        var m = project.Memberships.FirstOrDefault(x => x.Id == membershipId);

        if (m is null)
        {
            return NotFound();
        }

        var dto = new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Email, m.User.Created, m.User.Deleted),
            m.From, m.Thru);

        return Ok(dto);
    }

    [HttpPost("{id}/Memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectMembershipDto>> CreateProjectMembership(string id, CreateProjectMembershipDto createProjectMembershipDto)
    {
        var project = await context.Projects
            .Include(p => p.Memberships)
            .Include(p => p.Memberships)
            .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Id == createProjectMembershipDto.UserId);

        if (user is null)
        {
            return NotFound();
        }

        var membership = project.Memberships.FirstOrDefault(x => x.User.Id == user.Id);

        if (membership is not null)
        {
            return Problem(
                title: "User is already a member of this project",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var m = new ProjectMembership()
        {
            Id = Guid.NewGuid().ToString(),
            Project = project,
            User = user,
            From = createProjectMembershipDto.From,
            Thru = createProjectMembershipDto.Thru
        };

        context.ProjectMemberships.Add(m);

        await context.SaveChangesAsync();

        var dto = new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Email, m.User.Created, m.User.Deleted),
            m.From, m.Thru);

        return Ok(dto);
    }

    [HttpPut("{id}/Memberships/{membershipId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectMembershipDto>> UpdateProjectMembership(string id, string membershipId, UpdateProjectMembershipDto updateProjectMembershipDto)
    {
        var project = await context.Projects
            .Include(p => p.Memberships)
            .Include(p => p.Memberships)
            .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        var m = project.Memberships.FirstOrDefault(x => x.Id == membershipId);

        if (m is null)
        {
            return NotFound();
        }

        m.From = updateProjectMembershipDto.From;
        m.Thru = updateProjectMembershipDto.Thru;

        await context.SaveChangesAsync();

        var dto = new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Email, m.User.Created, m.User.Deleted),
            m.From, m.Thru);

        return Ok(dto);
    }

    [HttpDelete("{id}/Memberships/{membershipId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteProjectMembership(string id, string membershipId)
    {
        var project = await context.Projects
            .Include(p => p.Memberships)
            .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (project is null)
        {
            return NotFound();
        }

        var m = project.Memberships.FirstOrDefault(x => x.Id == membershipId);

        if (m is null)
        {
            return NotFound();
        }

        context.ProjectMemberships.Remove(m);

        await context.SaveChangesAsync();

        return Ok();
    }
}

public record class CreateProjectDto(string Name, string? Description);

public record class UpdateProjectDto(string Name, string? Description);

public record class StatisticsSummary(IEnumerable<StatisticsSummaryEntry> Entries);

public record class StatisticsSummaryEntry(string Name, double? Value, decimal? Value2 = null, string? unit = null);