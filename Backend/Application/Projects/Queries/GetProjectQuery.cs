
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Projects.Queries;

public class GetProjectQuery : IRequest<ProjectDto>
{
    public GetProjectQuery(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, ProjectDto>
    {
        private readonly ITimeReportContext _context;

        public GetProjectQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<ProjectDto> Handle(GetProjectQuery request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
               .Include(p => p.Memberships)
               .AsNoTracking()
               .AsSplitQuery()
               .FirstOrDefaultAsync(x => x.Id == request.Id);

            return new ProjectDto(project.Id, project.Name, project.Description);
        }
    }
}
