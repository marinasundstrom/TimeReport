
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

namespace TimeReport.Application.Activities;

public class DeleteActivityCommand : IRequest
{
    public DeleteActivityCommand(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class DeleteActivityCommandHandler : IRequestHandler<DeleteActivityCommand>
    {
        private readonly TimeReportContext _context;

        public DeleteActivityCommandHandler(TimeReportContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
        {
            var activity = await _context.Activities
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (activity is null)
            {
                throw new Exception();
            }

            _context.Activities.Remove(activity);

            await _context.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
