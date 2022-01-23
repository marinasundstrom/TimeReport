
using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Application;
using TimeReport.Application.Common.Interfaces;
using TimeReport.Application.Common.Models;
using TimeReport.Application.Projects;
using TimeReport.Application.Projects.Commands;
using TimeReport.Application.Projects.Queries;
using TimeReport.Application.Users;
using TimeReport.Domain.Entities;
using TimeReport.Domain.Exceptions;
using TimeReport.Dtos;

namespace TimeReport.Controllers;

[ApiController]
[Route("[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ProjectDto>>> GetProjects(string? userId = null, int page = 0, int pageSize = 10, string? searchString = null, string? sortBy = null, Application.Common.Models.SortDirection? sortDirection = null)
    {
        return Ok(await _mediator.Send(new GetProjectsQuery(page, pageSize, searchString, sortBy, sortDirection)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDto>> GetProject(string id)
    {
        var project = await _mediator.Send(new GetProjectQuery(id));

        if (project is null)
        {
            return NotFound();
        }

        return Ok(project);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto createProjectDto)
    {
        try
        {
            var project = await _mediator.Send(new CreateProjectCommand(createProjectDto.Name, createProjectDto.Description));

            return Ok(project);
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDto>> UpdateProject(string id, UpdateProjectDto updateProjectDto)
    {
        try
        {
            var project = await _mediator.Send(new UpdateProjectCommand(id, updateProjectDto.Name, updateProjectDto.Description));

            return Ok(project);
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteProject(string id)
    {
        try
        {
            await _mediator.Send(new DeleteProjectCommand(id));

            return Ok();
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpGet("Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary()
    {
        try
        {
            return Ok(await _mediator.Send(new GetProjectStatisticsSummaryQuery()));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpGet("Statistics")]
    public async Task<ActionResult<Data>> GetStatistics(DateTime? from = null, DateTime? to = null)
    {
        try
        {
            return Ok(await _mediator.Send(new GetProjectStatisticsQuery(from, to)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary(string id)
    {
        try
        {
            return Ok(await _mediator.Send(new GetProjectStatisticsSummaryForProjectQuery(id)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/Statistics")]
    public async Task<ActionResult<Data>> GetProjectStatistics(string id, DateTime? from = null, DateTime? to = null)
    {
        try
        {
            return Ok(await _mediator.Send(new GetProjectStatisticsForProjectQuery(id, from, to)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/Memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ProjectMembershipDto>>> GetProjectMemberships(string id, int page = 0, int pageSize = 10, string? sortBy = null, TimeReport.Application.Common.Models.SortDirection? sortDirection = null)
    {
        try
        {
            return Ok(await _mediator.Send(new GetProjectMembershipsQuery(id, page, pageSize, sortBy, sortDirection)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/Memberships/{membershipId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectMembershipDto>> GetProjectMembership(string id, string membershipId)
    {
        try
        {
            return Ok(await _mediator.Send(new GetProjectMembershipQuery(id, membershipId)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/Memberships")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectMembershipDto>> CreateProjectMembership(string id, CreateProjectMembershipDto createProjectMembershipDto)
    {
        try
        {
            return Ok(await _mediator.Send(new CreateProjectMembershipCommand(id, createProjectMembershipDto.UserId, createProjectMembershipDto.From, createProjectMembershipDto.Thru)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/Memberships/{membershipId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectMembershipDto>> UpdateProjectMembership(string id, string membershipId, UpdateProjectMembershipDto updateProjectMembershipDto)
    {
        try
        {
            return Ok(await _mediator.Send(new UpdateProjectMembershipCommand(id, membershipId, updateProjectMembershipDto.From, updateProjectMembershipDto.Thru)));
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
        catch (ProjectMembershipNotFoundException exc)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}/Memberships/{membershipId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteProjectMembership(string id, string membershipId)
    {
        try
        {
            await _mediator.Send(new DeleteProjectMembershipCommand(id, membershipId));

            return Ok();
        }
        catch (ProjectNotFoundException exc)
        {
            return NotFound();
        }
        catch (ProjectMembershipNotFoundException exc)
        {
            return NotFound();
        }
    }
}

public record class CreateProjectDto(string Name, string? Description);

public record class UpdateProjectDto(string Name, string? Description);