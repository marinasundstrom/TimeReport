using System;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Projects;

public class GetProjectStatisticsSummaryQuery : IRequest<StatisticsSummary>
{
    public GetProjectStatisticsSummaryQuery(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class GetProjectStatisticsSummaryQueryHandler : IRequestHandler<GetProjectStatisticsSummaryQuery, StatisticsSummary>
    {
        private readonly ITimeReportContext _context;

        public GetProjectStatisticsSummaryQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<StatisticsSummary> Handle(GetProjectStatisticsSummaryQuery request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Entries)
                .ThenInclude(x => x.User)
                .Include(p => p.Entries)
                .ThenInclude(x => x.Activity)
                .Include(p => p.Expenses)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.Id);

            if (project is null)
            {
                throw new Exception();
            }

            var totalHours = project.Entries
                .Sum(e => e.Hours.GetValueOrDefault());

            var revenue = project.Entries
                .Where(e => e.Activity.HourlyRate.GetValueOrDefault() > 0)
                .Sum(e => e.Activity.HourlyRate.GetValueOrDefault() * (decimal)e.Hours.GetValueOrDefault());

            var expenses = project.Entries
                 .Where(e => e.Activity.HourlyRate.GetValueOrDefault() < 0)
                 .Sum(e => e.Activity.HourlyRate.GetValueOrDefault() * (decimal)e.Hours.GetValueOrDefault());

            expenses -= project.Expenses
                 .Sum(e => e.Amount);

            var totalUsers = project.Entries
                .Select(e => e.User)
                .DistinctBy(e => e.Id)
                .Count();

            return new StatisticsSummary(new StatisticsSummaryEntry[]
            {
                new ("Participants", totalUsers),
                new ("Hours", totalHours),
                new ("Revenue", null, revenue, unit: "currency"),
                new ("Expenses", null, expenses, unit: "currency")
            });
        }
    }
}