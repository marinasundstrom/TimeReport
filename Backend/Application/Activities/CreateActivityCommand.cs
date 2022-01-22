
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Controllers;
using TimeReport.Data;

namespace TimeReport.Application.Activities;

public class CreateActivityCommand : IRequest<ActivityDto>
{
    public CreateActivityCommand(string projectId, string name, string? description, decimal? hourlyRate)
    {
        ProjectId = projectId;
        Name = name;
        Description = description;
        HourlyRate = hourlyRate;
    }

    public string ProjectId { get; }
    public string Name { get; }
    public string? Description { get; }
    public decimal? HourlyRate { get; }

    public class CreateActivityCommandHandler : IRequestHandler<CreateActivityCommand, ActivityDto>
    {
        private readonly TimeReportContext _context;

        public CreateActivityCommandHandler(TimeReportContext context)
        {
            _context = context;
        }

        public async Task<ActivityDto> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
               .AsSplitQuery()
               .FirstOrDefaultAsync(x => x.Id == request.ProjectId);

            if (project is null)
            {
                throw new Exception();
            }

            var activity = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Project = project,
                HourlyRate = request.HourlyRate
            };

            _context.Activities.Add(activity);

            await _context.SaveChangesAsync();

            return new ActivityDto(activity.Id, activity.Name, activity.Description, activity.HourlyRate, new ProjectDto(activity.Project.Id, activity.Project.Name, activity.Project.Description));
        }
    }
}
