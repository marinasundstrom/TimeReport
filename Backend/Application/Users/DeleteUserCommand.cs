
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

namespace TimeReport.Application.Users;

public class DeleteUserCommand : IRequest
{
    public DeleteUserCommand(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly TimeReportContext _context;

        public DeleteUserCommandHandler(TimeReportContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (user is null)
            {
                throw new Exception();
            }

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return Unit.Value;
        }
    }
}
