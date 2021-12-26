
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly TimeReportContext context;

    public UsersController(TimeReportContext context)
    {
        this.context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<UserDto>>> GetUsers(int page = 0, int pageSize = 10)
    {
        var totalItems = await context.Users.CountAsync();

        var users = await context.Users
            .OrderBy(p => p.Created)
            .Skip(pageSize * page)
            .Take(pageSize)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        var dtos = users.Select(user => new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Created, user.Deleted));

        return Ok(new ItemsResult<UserDto>(dtos, totalItems));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await context.Users
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        var dto = new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Created, user.Deleted);
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            DisplayName = createUserDto.DisplayName,
            SSN = createUserDto.SSN
        };

        context.Users.Add(user);

        await context.SaveChangesAsync();

        var dto = new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Created, user.Deleted);
        return Ok(dto);
    }

    [HttpPut("{id}/Details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> UpdateUser(string id, UpdateUserDetailsDto updateUserDetailsDto)
    {
        var user = await context.Users
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        user.FirstName = updateUserDetailsDto.FirstName;
        user.LastName = updateUserDetailsDto.LastName;
        user.DisplayName = updateUserDetailsDto.DisplayName;
        user.SSN = updateUserDetailsDto.SSN;

        await context.SaveChangesAsync();

        var dto = new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Created, user.Deleted);
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteUser(string id)
    {
        var user = await context.Users
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        context.Users.Remove(user);

        await context.SaveChangesAsync();

        return Ok();
    }


    [HttpGet("{id}/Memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ProjectMembershipDto>>> GetProjectMemberships(string id, int page = 0, int pageSize = 10)
    {
        var query = context.ProjectMemberships
            .OrderBy(p => p.Created)
            .Where(x => x.User.Id == id);

        var totalItems = await context.ProjectMemberships.CountAsync();

        var projectMemberships = await query
            .Include(m => m.Project)
            .Include(m => m.User)
            .Skip(pageSize * page)
            .Take(pageSize)
            .AsSplitQuery()
            .ToArrayAsync();

        var dtos = projectMemberships
            .DistinctBy(x => x.Project) // Temp
            .Select(m => new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Created, m.User.Deleted),
            m.From, m.Thru));

        return Ok(new ItemsResult<ProjectMembershipDto>(dtos, totalItems));
    }

    [HttpGet("{id}/Statistics")]
    public async Task<ActionResult<Data>> GetStatistics(string id, DateTime? from = null, DateTime? to = null)
    {
        var projects = await context.Projects
            .Include(x => x.Memberships)
            .ThenInclude(x => x.User)
            .Include(x => x.Activities)
            .ThenInclude(x => x.Entries)
            .ThenInclude(x => x.User)
            .Where(x => x.Memberships.Any(x => x.User.Id == id))
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
                    .Where(e => e.User.Id == id)
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
}

public record class CreateUserDto(string FirstName, string LastName, string? DisplayName, string SSN);

public record class UpdateUserDetailsDto(string FirstName, string LastName, string? DisplayName, string SSN);