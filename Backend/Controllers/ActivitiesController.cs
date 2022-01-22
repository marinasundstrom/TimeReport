﻿
using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Activities;
using TimeReport.Application.Common.Interfaces;
using TimeReport.Infrastructure;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITimeReportContext context;

    public ActivitiesController(IMediator mediator, ITimeReportContext context)
    {
        _mediator = mediator;
        this.context = context;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ActivityDto>>> GetActivities(int page = 0, int pageSize = 10, string? projectId = null, string? searchString = null, string? sortBy = null, TimeReport.SortDirection? sortDirection = null)
    {
        return Ok(await _mediator.Send(new GetActivitiesQuery(page, pageSize, projectId, searchString, sortBy, sortDirection)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> GetActivity(string id)
    {
        var activity = await _mediator.Send(new GetActivityQuery(id));

        if (activity is null)
        {
            return NotFound();
        }

        return Ok(activity);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> CreateActivity(string projectId, CreateActivityDto createActivityDto)
    {
        try
        {
            var activity = await _mediator.Send(new CreateActivityCommand(projectId, createActivityDto.Name, createActivityDto.Description, createActivityDto.HourlyRate));

            return Ok(activity);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ActivityDto>> UpdateActivity(string id, UpdateActivityDto updateActivityDto)
    {
        try
        {
            var activity = await _mediator.Send(new UpdateActivityCommand(id, updateActivityDto.Name, updateActivityDto.Description, updateActivityDto.HourlyRate));

            return Ok(activity);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteActivity(string id)
    {
        try
        {
            var activity = await _mediator.Send(new DeleteActivityCommand(id));

            return Ok();
        }
        catch (Exception)
        {
            return NotFound();
        }
    }


    [HttpGet("{id}/Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary(string id)
    {
        try
        {
            var statistics = await _mediator.Send(new GetActivityStatisticsSummaryQuery(id));

            return Ok(_mediator.Send(new GetActivityStatisticsSummaryQuery(id)));
        }
        catch (Exception)
        {
            return NotFound();
        }
    }
}

public record class CreateActivityDto(string Name, string? Description, decimal? HourlyRate);

public record class UpdateActivityDto(string Name, string? Description, decimal? HourlyRate);