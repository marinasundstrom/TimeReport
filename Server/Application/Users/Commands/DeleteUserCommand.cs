﻿
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;

namespace TimeReport.Application.Users.Commands;

public class DeleteUserCommand : IRequest
{
    public DeleteUserCommand(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly ITimeReportContext _context;

        public DeleteUserCommandHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(x => x.Id == request.UserId);

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