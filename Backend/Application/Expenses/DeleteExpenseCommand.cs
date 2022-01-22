using System;

using MediatR;

using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

namespace TimeReport.Application.Expenses;

public class DeleteExpenseCommand : IRequest
{
    public DeleteExpenseCommand(string id)
    {
        Id = id;
    }

    public string Id { get; }

    public class DeleteExpenseCommandHandler : IRequestHandler<DeleteExpenseCommand>
    {
        private readonly TimeReportContext _context;

        public DeleteExpenseCommandHandler(TimeReportContext context)
        {
            _context = context;
        }

        public async Task<Unit> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
        {
            var expense = await _context.Expenses
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.Id == request.Id);

            if (expense is null)
            {
                throw new Exception();
            }

            _context.Expenses.Remove(expense);

            await _context.SaveChangesAsync();

            return Unit.Value;
        }
    }
}