
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Application.Common.Models;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Users.Queries;

public class GetUsersQuery : IRequest<ItemsResult<UserDto>>
{
    public GetUsersQuery(int page = 0, int pageSize = 10, string? searchString = null, string? sortBy = null, SortDirection? sortDirection = null)
    {
        Page = page;
        PageSize = pageSize;
        SearchString = searchString;
        SortBy = sortBy;
        SortDirection = sortDirection;
    }

    public int Page { get; }

    public int PageSize { get; }

    public string? UserId { get; }

    public string? SearchString { get; }

    public string? SortBy { get; }

    public SortDirection? SortDirection { get; }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ItemsResult<UserDto>>
    {
        private readonly ITimeReportContext _context;

        public GetUsersQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<ItemsResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users
                .OrderBy(p => p.Created)
                .Skip(request.PageSize * request.Page)
                .Take(request.PageSize)
                .AsNoTracking()
                .AsSplitQuery();

            if (request.SearchString is not null)
            {
                query = query.Where(p =>
                p.FirstName.ToLower().Contains(request.SearchString.ToLower())
                || p.LastName.ToLower().Contains(request.SearchString.ToLower())
                || ((p.DisplayName ?? "").ToLower().Contains(request.SearchString.ToLower()))
                || p.SSN.ToLower().Contains(request.SearchString.ToLower())
                || p.Email.ToLower().Contains(request.SearchString.ToLower()));
            }

            var totalItems = await query.CountAsync();

            if (request.SortBy is not null)
            {
                query = query.OrderBy(request.SortBy, request.SortDirection == TimeReport.SortDirection.Descending ? TimeReport.SortDirection.Descending : TimeReport.SortDirection.Ascending);
            }

            var users = await query.ToListAsync();

            var dtos = users.Select(user => new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Email, user.Created, user.Deleted));

            return new ItemsResult<UserDto>(dtos, totalItems);
        }
    }
}
