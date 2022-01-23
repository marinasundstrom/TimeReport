﻿
using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;

namespace TimeReport.Application.Users.Queries;

public class GetUserQuery : IRequest<UserDto>
{
    public GetUserQuery(string userId)
    {
        UserId = userId;
    }

    public string UserId { get; }

    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
    {
        private readonly ITimeReportContext _context;

        public GetUserQueryHandler(ITimeReportContext context)
        {
            _context = context;
        }

        public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.UserId);

            if (user is null)
            {
                throw new Exception();
            }

            return new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.SSN, user.Email, user.Created, user.Deleted);
        }
    }
}