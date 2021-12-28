
using Azure.Storage.Blobs;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

namespace TimeReport.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly TimeReportContext context;
    private readonly BlobServiceClient blobServiceClient;

    public ExpensesController(TimeReportContext context, BlobServiceClient blobServiceClient)
    {
        this.context = context;
        this.blobServiceClient = blobServiceClient;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ItemsResult<ExpenseDto>>> GetExpenses(int page = 0, int pageSize = 10, string? projectId = null, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null)
    {
        var query = context.Expenses
            .Include(x => x.Project)
            .OrderBy(p => p.Created)
            .AsNoTracking()
            .AsSplitQuery();

        if (projectId is not null)
        {
            query = query.Where(expense => expense.Project.Id == projectId);
        }

        if (searchString is not null)
        {
            query = query.Where(expense => expense.Description.ToLower().Contains(searchString.ToLower()));
        }

        var totalItems = await query.CountAsync();

        if (sortBy is not null)
        {
            query = query.OrderBy(sortBy, sortDirection == SortDirection.Desc ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
        }

        var expenses = await query
            .Skip(pageSize * page)
            .Take(pageSize)   
            .ToListAsync();

        var dtos = expenses.Select(expense => new ExpenseDto(expense.Id, expense.Date.ToDateTime(TimeOnly.Parse("1:00")), expense.Amount, expense.Description, GetAttachmentUrl(expense.Attachment), new ProjectDto(expense.Project.Id, expense.Project.Name, expense.Project.Description)));
        
        return Ok(new ItemsResult<ExpenseDto>(dtos, totalItems));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> GetExpense(string id)
    {
        var expense = await context.Expenses
            .Include(x => x.Project)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (expense is null)
        {
            return NotFound();
        }

        var dto = new ExpenseDto(expense.Id, expense.Date.ToDateTime(TimeOnly.Parse("1:00")), expense.Amount, expense.Description, GetAttachmentUrl(expense.Attachment), new ProjectDto(expense.Project.Id, expense.Project.Name, expense.Project.Description));
        return Ok(dto);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> CreateExpense(string projectId, CreateExpenseDto createExpenseDto)
    {
        var project = await context.Projects
           .AsSplitQuery()
           .FirstOrDefaultAsync(x => x.Id == projectId);

        if (project is null)
        {
            return NotFound();
        }

        var expense = new Expense
        {
            Id = Guid.NewGuid().ToString(),
            Type = ExpenseType.Purchase,
            Date = DateOnly.FromDateTime(createExpenseDto.Date),
            Amount = createExpenseDto.Amount,
            Description = createExpenseDto.Description,
            Project = project
        };

        context.Expenses.Add(expense);

        await context.SaveChangesAsync();

        var dto = new ExpenseDto(expense.Id, expense.Date.ToDateTime(TimeOnly.Parse("1:00")), expense.Amount, expense.Description, GetAttachmentUrl(expense.Attachment), new ProjectDto(expense.Project.Id, expense.Project.Name, expense.Project.Description));
        return Ok(dto);
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

    private static string? GetAttachmentUrl(string? name)
    {
        if (name is null) return null;

        return name is null ? null : $"http://127.0.0.1:10000/devstoreaccount1/attachments/{name}";
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpenseDto>> UpdateExpense(string id, UpdateExpenseDto updateExpenseDto)
    {
        var expense = await context.Expenses
            .Include(x => x.Project)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (expense is null)
        {
            return NotFound();
        }

        expense.Date = DateOnly.FromDateTime(updateExpenseDto.Date);
        expense.Amount = updateExpenseDto.Amount;
        expense.Description = updateExpenseDto.Description;

        await context.SaveChangesAsync();

        var dto = new ExpenseDto(expense.Id, expense.Date.ToDateTime(TimeOnly.Parse("1:00")), expense.Amount, expense.Description, GetAttachmentUrl(expense.Attachment), new ProjectDto(expense.Project.Id, expense.Project.Name, expense.Project.Description));
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> DeleteExpense(string id)
    {
        var expense = await context.Expenses
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (expense is null)
        {
            return NotFound();
        }

        context.Expenses.Remove(expense);

        await context.SaveChangesAsync();

        return Ok();
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