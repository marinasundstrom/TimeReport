﻿
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Controllers;
using TimeReport.Infrastructure;

namespace TimeReport.Application.Users;

public class UpdateUserCommand : IRequest<UserDto>
{
    public UpdateUserCommand(string id, string firstName, string lastName, string? displayName, string ssn, string email)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        Ssn = ssn;
        Email = email;
    }

    public string Id { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public string? DisplayName { get; }

    public string Ssn { get; }

    public string Email { get; }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
    {
        private readonly ITimeReportContext _context;

        public UpdateUserCommandHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (user is null)
            {
                throw new Exception();
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.DisplayName = request.DisplayName;
            user.SSN = request.Ssn;
            user.Email = request.Email;

            await _context.SaveChangesAsync();

            return new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Email, user.Created, user.Deleted);
        }
    }
}
