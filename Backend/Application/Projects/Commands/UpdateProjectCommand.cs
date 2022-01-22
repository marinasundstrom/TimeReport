
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Projects.Commands;

public class UpdateProjectCommand : IRequest<ProjectDto>
{
    public UpdateProjectCommand(string id, string name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }

    public string Id { get; }

    public string Name { get; }

    public string? Description { get; }

    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
    {
        private readonly ITimeReportContext _context;

        public UpdateProjectCommandHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (project is null)
            {
                throw new Exception();
            }

            project.Name = request.Name;
            project.Description = request.Description;

            await _context.SaveChangesAsync();

            return new ProjectDto(project.Id, project.Name, project.Description);
        }
    }
}
