
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Controllers;
using TimeReport.Data;

namespace TimeReport.Application.Activities;

public class GetActivityStatisticsSummaryQuery : IRequest<StatisticsSummary>
{
    public GetActivityStatisticsSummaryQuery(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class GetStatisticsSummaryQueryHandler : IRequestHandler<GetActivityStatisticsSummaryQuery, StatisticsSummary>
    {
        private readonly TimeReportContext _context;

        public GetStatisticsSummaryQueryHandler(TimeReportContext context)
        {
            _context = context;
        }

        public async Task<StatisticsSummary> Handle(GetActivityStatisticsSummaryQuery request, CancellationToken cancellationToken)
        {
            var activity = await _context.Activities
               .Include(x => x.Entries)
               .ThenInclude(x => x.User)
               .AsSplitQuery()
               .AsNoTracking()
               .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (activity is null)
            {
                throw new Exception();
            }

            var totalHours = activity.Entries
                .Sum(p => p.Hours.GetValueOrDefault());

            var totalUsers = activity.Entries
                .Select(p => p.User)
                .DistinctBy(p => p.Id)
                .Count();

            return new StatisticsSummary(new StatisticsSummaryEntry[]
            {
                new ("Participants", totalUsers),
                new ("Hours", totalHours)
            });
        }
    }
}