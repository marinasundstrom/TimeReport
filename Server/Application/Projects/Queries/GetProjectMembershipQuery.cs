
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Application.Users;
using TimeReport.Domain.Exceptions;

namespace TimeReport.Application.Projects.Queries;

public class GetProjectMembershipQuery : IRequest<ProjectMembershipDto>
{
    public GetProjectMembershipQuery(string id, string membershipId)
    {
        Id = id;
        MembershipId = membershipId;
    }

    public string Id { get; }
    public string MembershipId { get; }

    public class GetProjectMembershipQueryHandler : IRequestHandler<GetProjectMembershipQuery, ProjectMembershipDto>
    {
        private readonly ITimeReportContext _context;

        public GetProjectMembershipQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<ProjectMembershipDto> Handle(GetProjectMembershipQuery request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .Include(p => p.Memberships)
                .Include(p => p.Memberships)
                .ThenInclude(m => m.User)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (project is null)
            {
                throw new ProjectNotFoundException(request.Id);
            }

            var m = project.Memberships.FirstOrDefault(x => x.Id == request.MembershipId);

            if (m is null)
            {
                throw new ProjectMembershipNotFoundException(request.Id);
            }

            return new ProjectMembershipDto(m.Id, new ProjectDto(m.Project.Id, m.Project.Name, m.Project.Description),
                new UserDto(m.User.Id, m.User.FirstName, m.User.LastName, m.User.DisplayName, m.User.SSN, m.User.Email, m.User.Created, m.User.Deleted),
                m.From, m.Thru);
        }
    }
}