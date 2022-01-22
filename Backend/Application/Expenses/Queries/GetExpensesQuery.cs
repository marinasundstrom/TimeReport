
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Application.Common.Models;
using TimeReport.Application.Projects;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

using static TimeReport.Application.Expenses.ExpensesHelpers;

namespace TimeReport.Application.Expenses.Queries;

public class GetExpensesQuery : IRequest<ItemsResult<ExpenseDto>>
{
    public GetExpensesQuery(int page = 0, int pageSize = 10, string? projectId = null, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null)
    {
        Page = page;
        PageSize = pageSize;
        ProjectId = projectId;
        SearchString = searchString;
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public int Page { get; }

    public int PageSize { get; }

    public string? ProjectId { get; }

    public string? SearchString { get; }

    public string? SortBy { get; }

    public SortDirection? SortDirection { get; }

    public class GetExpensesQueryHandler : IRequestHandler<GetExpensesQuery, ItemsResult<ExpenseDto>>
    {
        private readonly ITimeReportContext _context;

        public GetExpensesQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<ItemsResult<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Expenses
                .Include(x => x.Project)
                .OrderBy(p => p.Created)
                .AsNoTracking()
                .AsSplitQuery();

            if (request.ProjectId is not null)
            {
                query = query.Where(expense => expense.Project.Id == request.ProjectId);
            }

            if (request.SearchString is not null)
            {
                query = query.Where(expense => expense.Description.ToLower().Contains(request.SearchString.ToLower()));
            }

            var totalItems = await query.CountAsync();

            if (request.SortBy is not null)
            {
                query = query.OrderBy(request.SortBy, request.SortDirection == TimeReport.SortDirection.Descending ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
            }

            var expenses = await query
                .Skip(request.PageSize * request.Page)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = expenses.Select(expense => new ExpenseDto(expense.Id, expense.Date.ToDateTime(TimeOnly.Parse("1:00")), expense.Amount, expense.Description, GetAttachmentUrl(expense.Attachment), new ProjectDto(expense.Project.Id, expense.Project.Name, expense.Project.Description)));

            return new ItemsResult<ExpenseDto>(dtos, totalItems);
        }
    }
}
