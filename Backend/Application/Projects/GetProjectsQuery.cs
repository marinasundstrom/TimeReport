
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Controllers;
using TimeReport.Data;

namespace TimeReport.Application.Projects;

public class GetProjectsQuery : IRequest<ItemsResult<ProjectDto>>
{
    public GetProjectsQuery(int page = 0, int pageSize = 10, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null)
    {
        Page = page;
        PageSize = pageSize;
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

    public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, ItemsResult<ProjectDto>>
    {
        private readonly TimeReportContext _context;

        public GetProjectsQueryHandler(TimeReportContext context)
        {
            _context = context;
        }

        public async Task<ItemsResult<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Projects
                .Include(p => p.Memberships)
                .OrderBy(p => p.Created)
                .AsNoTracking()
                .AsSplitQuery();

            if (request.SearchString is not null)
            {
                query = query.Where(project => project.Description.ToLower().Contains(request.SearchString.ToLower()));
            }

            var totalItems = await query.CountAsync();

            if (request.SortBy is not null)
            {
                query = query.OrderBy(request.SortBy, request.SortDirection == TimeReport.SortDirection.Descending ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
            }

            var projects = await query
                .Skip(request.PageSize * request.Page)
                .Take(request.PageSize)
                .ToListAsync();

            var dtos = projects.Select(project => new ProjectDto(project.Id, project.Name, project.Description));

            return new ItemsResult<ProjectDto>(dtos, totalItems);
        }
    }
}
