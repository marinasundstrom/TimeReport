using System;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Users;

public class GetUserStatisticsSummaryQuery : IRequest<StatisticsSummary>
{
    public GetUserStatisticsSummaryQuery(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class GetUserStatisticsSummaryQueryHandler : IRequestHandler<GetUserStatisticsSummaryQuery, StatisticsSummary>
    {
        private readonly ITimeReportContext _context;

        public GetUserStatisticsSummaryQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<StatisticsSummary> Handle(GetUserStatisticsSummaryQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                       .AsNoTracking()
                       .AsSplitQuery()
                       .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (user is null)
            {
                throw new Exception();
            }

            var entries = await _context.Entries
                .Include(x => x.Project)
                .Where(x => x.User.Id == request.Id)
                .AsSplitQuery()
                .AsNoTracking()
                .ToArrayAsync();

            var totalHours = entries
                .Sum(p => p.Hours.GetValueOrDefault());

            var totalProjects = entries
                .Select(p => p.Project)
                .DistinctBy(p => p.Id)
                .Count();

            return new StatisticsSummary(new StatisticsSummaryEntry[]
            {
                new ("Projects", totalProjects),
                new ("Hours", totalHours)
            });
        }
    }
}