﻿
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Activities;

public class GetActivityQuery : IRequest<ActivityDto>
{
    public GetActivityQuery(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class GetActivityQueryHandler : IRequestHandler<GetActivityQuery, ActivityDto>
    {
        private readonly ITimeReportContext _context;

        public GetActivityQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<ActivityDto> Handle(GetActivityQuery request, CancellationToken cancellationToken)
        {
            var activity = await _context.Activities
               .Include(x => x.Project)
               .AsNoTracking()
               .AsSplitQuery()
               .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (activity is null)
            {
                throw new Exception();
            }

            return new ActivityDto(activity.Id, activity.Name, activity.Description, activity.HourlyRate, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description));
        }
    }
}
