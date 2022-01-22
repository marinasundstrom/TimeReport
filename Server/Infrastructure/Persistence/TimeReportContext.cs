
using Microsoft.EntityFrameworkCore;

using TimeReport.Application.Common.Interfaces;
using TimeReport.Domain.Common.Interfaces;
using TimeReport.Domain.Entities;
using TimeReport.Infrastructure.Persistence.Configurations;

namespace TimeReport.Infrastructure.Persistence;

public class TimeReportContext : DbContext, ITimeReportContext
{
    private readonly ICurrentUserService _currentUserService;

    public TimeReportContext(DbContextOptions<TimeReportContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Project> Projects { get; set; } = null!;

    public DbSet<ProjectMembership> ProjectMemberships { get; set; } = null!;

    public DbSet<Expense> Expenses { get; set; } = null!;

    public DbSet<Activity> Activities { get; set; } = null!;

    public DbSet<Entry> Entries { get; set; } = null!;

    public DbSet<TimeSheet> TimeSheets { get; set; } = null!;

    public DbSet<MonthEntryGroup> MonthEntryGroups { get; set; } = null!;

    public DbSet<TimeSheetActivity> TimeSheetActivities { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    entry.Entity.Created = DateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedBy = _currentUserService.UserId;
                    entry.Entity.LastModified = DateTime.Now;
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        softDelete.DeletedBy = _currentUserService.UserId;
                        softDelete.Deleted = DateTime.Now;

                        entry.State = EntityState.Modified;
                    }
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        return result;
    }
}