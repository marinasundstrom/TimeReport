
using Azure.Storage.Blobs;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Application.Common.Models;
using TimeReport.Application.Expenses;
using TimeReport.Application.Expenses.Commands;
using TimeReport.Application.Expenses.Queries;
using TimeReport.Infrastructure;

using static TimeReport.Application.Expenses.ExpensesHelpers;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ITimeReportContext context;
    private readonly BlobServiceClient blobServiceClient;

    public ExpensesController(IMediator mediator, ITimeReportContext context, BlobServiceClient blobServiceClient)
    {
        _mediator = mediator;
        this.context = context;
        this.blobServiceClient = blobServiceClient;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ExpenseDto>>> GetExpenses(int page = 0, int pageSize = 10, string? projectId = null, string? searchString = null, string? sortBy = null, TimeReport.SortDirection? sortDirection = null)
    {
        return Ok(await _mediator.Send(new GetExpensesQuery(page, pageSize, projectId, searchString, sortBy, sortDirection)));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> GetExpense(string id)
    {
        var expense = await _mediator.Send(new GetExpenseQuery(id));

        if (expense is null)
        {
            return NotFound();
        }

        return Ok(expense);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(string projectId, CreateExpenseDto createExpenseDto)
    {
        try
        {
            var activity = await _mediator.Send(new CreateExpenseCommand(projectId, createExpenseDto.Date, createExpenseDto.Amount, createExpenseDto.Description));

            return Ok(activity);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/Attachment")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<ActionResult> UploadAttachment([FromRoute] string id, IFormFile file)
    {
        var stream = file.OpenReadStream();

        var expense = await context.Expenses
            .Include(x => x.Project)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (expense is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(expense.Attachment))
        {
            return Problem(title: "Attachment could not be set", detail: "There is already an attachment for this expense.");
        }

        var blobContainerClient = blobServiceClient.GetBlobContainerClient("attachments");

#if DEBUG
        await blobContainerClient.CreateIfNotExistsAsync();
#endif

        var blobName = $"{expense.Id}-{file.FileName}";

        var response = await blobContainerClient.UploadBlobAsync(blobName, file.OpenReadStream());

        expense.Attachment = blobName;

        await context.SaveChangesAsync();

        var url = GetAttachmentUrl(expense.Attachment);

        return Ok(url);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> UpdateExpense(string id, UpdateExpenseDto updateExpenseDto)
    {
        try
        {
            var activity = await _mediator.Send(new UpdateExpenseCommand(id, updateExpenseDto.Date, updateExpenseDto.Amount, updateExpenseDto.Description));

            return Ok(activity);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteExpense(string id)
    {
        try
        {
            var activity = await _mediator.Send(new DeleteExpenseCommand(id));

            return Ok();
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    /*
    [HttpGet("{id}/Statistics/Summary")]
    public async Task<ActionResult<StatisticsSummary>> GetStatisticsSummary(string id)
    {
        var expense = await context.Expenses
            .Include(x => x.Entries)
            .ThenInclude(x => x.User)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (expense is null)
        {
            return NotFound();
        }

        var totalHours = expense.Entries
            .Sum(p => p.Hours.GetValueOrDefault());

        var totalUsers = expense.Entries
            .Select(p => p.User)
            .DistinctBy(p => p.Id)
            .Count();

        return new StatisticsSummary(new StatisticsSummaryEntry[]
        {
            new ("Participants", totalUsers),
            new ("Hours", totalHours)
        });
    }
    */
}

public record class CreateExpenseDto(DateTime Date, decimal Amount, string? Description);

public record class UpdateExpenseDto(DateTime Date, decimal Amount, string? Description);