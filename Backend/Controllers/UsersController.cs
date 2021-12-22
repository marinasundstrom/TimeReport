﻿
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
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await context.Users
            .OrderBy(p => p.Created)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync();

        var dto = users.Select(user => new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Created, user.Deleted));
        return Ok(dto);
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
    public async Task<ActionResult<IEnumerable<ProjectMembershipDto>>> GetProjectMemberships(string id)
    {
        var projectMemberships = await context.ProjectMemberships
            .Include(m => m.Project)
            .Include(m => m.User)
            .OrderBy(p => p.Created)
            .AsSplitQuery()
            .Where(x => x.User.Id == id)
            .ToArrayAsync();

        var dto = projectMemberships
            .DistinctBy(x => x.Project) // Temp
            .Select(m => new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
            new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Created, m.User.Deleted),
            m.From, m.Thru));

        return Ok(dto);
    }
}

public record class CreateUserDto(string FirstName, string LastName, string? DisplayName, string SSN);

public record class UpdateUserDetailsDto(string FirstName, string LastName, string? DisplayName, string SSN);