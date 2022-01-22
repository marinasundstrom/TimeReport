
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Projects.Commands;

public class DeleteProjectCommand : IRequest
{
    public DeleteProjectCommand(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
    {
        private readonly ITimeReportContext _context;

        public DeleteProjectCommandHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _context.Projects
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (project is null)
            {
                throw new Exception();
            }

            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
