
using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Application;
using TimeReport.Application.Common.Interfaces;
using TimeReport.Application.Common.Models;
using TimeReport.Application.Projects;
using TimeReport.Application.Users;
using TimeReport.Application.Users.Commands;
using TimeReport.Application.Users.Queries;

namespace TimeReport.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITimeReportContext context;

    public UsersController(IMediator mediator, ITimeReportContext context)
    {
        _mediator = mediator;
        this.context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<UserDto>>> GetUsers(int page = 0, int pageSize = 10, string? searchString = null, string? sortBy = null, TimeReport.Application.Common.Models.SortDirection? sortDirection = null)
    {
        return Ok(await _mediator.Send(new GetUsersQuery(page, pageSize, searchString, sortBy, sortDirection)));

    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _mediator.Send(new GetUserQuery(id));

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto createUserDto)
    {
        try
        {
            var user = await _mediator.Send(new CreateUserCommand(createUserDto.FirstName, createUserDto.LastName, createUserDto.DisplayName, createUserDto.SSN, createUserDto.Email));

            return Ok(user);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/Details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> UpdateUser(string id, UpdateUserDetailsDto updateUserDetailsDto)
    {
        try
        {
            var user = await _mediator.Send(new UpdateUserCommand(id, updateUserDetailsDto.FirstName, updateUserDetailsDto.LastName, updateUserDetailsDto.DisplayName, updateUserDetailsDto.SSN, updateUserDetailsDto.Email));

            return Ok(user);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            await _mediator.Send(new DeleteUserCommand(id));

            return Ok();
        }
        catch (Exception)
        {
            return NotFound();
        }
    }


    [HttpGet("{id}/Memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ProjectMembershipDto>>> GetProjectMemberships(string id, int page = 0, int pageSize = 10, string? sortBy = null, Application.Common.Models.SortDirection? sortDirection = null)
    {
        var query = context.ProjectMemberships
            .OrderBy(p => p.Created)
            .Where(x => x.User.Id == id);

        var totalItems = await query.CountAsync();

        if (sortBy is not null)
        {
            query = query.OrderBy(sortBy, sortDirection == Application.Common.Models.SortDirection.Desc ? TimeReport.Application.SortDirection.Descending : TimeReport.Application.SortDirection.Ascending);
        }

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
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Email, m.User.Created, m.User.Deleted),
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

    [HttpGet("{id}/Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary(string id)
    {
        try
        {
            return Ok(await _mediator.Send(new GetUserStatisticsSummaryQuery(id)));
        }
        catch (Exception)
        {
            return NotFound();
        }
    }
}

public record class CreateUserDto(string FirstName, string LastName, string? DisplayName, string SSN, string Email);

public record class UpdateUserDetailsDto(string FirstName, string LastName, string? DisplayName, string SSN, string Email);