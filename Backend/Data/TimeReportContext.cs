using System;

using Microsoft.EntityFrameworkCore;

using TimeReport.Services;

namespace TimeReport.Data;

public class TimeReportContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public TimeReportContext(DbContextOptions<TimeReportContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.IsTemporal());
            e.HasQueryFilter(i => i.Deleted == null);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("Projects", t => t.IsTemporal());
            e.HasQueryFilter(i => i.Deleted == null);
        });

        modelBuilder.Entity<ProjectMembership>(e =>
        {
            e.ToTable("ProjectMemberships", t => t.IsTemporal());
            e.HasQueryFilter(i => i.Deleted == null);
        });
        modelBuilder.Entity<Activity>(e =>
        {
            e.ToTable("Activities", t => t.IsTemporal());
            e.HasQueryFilter(i => i.Deleted == null);
        });

        modelBuilder.Entity<Entry>(e =>
        {
            e.Property(x => x.Date)
                .HasConversion(x => x.ToDateTime(TimeOnly.Parse("01:00")), x => DateOnly.FromDateTime(x));

            e.ToTable("Entries", t => t.IsTemporal());
        });

        modelBuilder.Entity<TimeSheet>(e =>
        {
            e.ToTable("TimeSheets", t => t.IsTemporal());
            e.HasQueryFilter(i => i.Deleted == null);
        });

        modelBuilder.Entity<TimeSheetActivity>(e =>
        {
            e.ToTable("TimeSheetActivities", t => t.IsTemporal());
            e.HasQueryFilter(i => i.Deleted == null);
        });
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Project> Projects { get; set; } = null!;

    public DbSet<ProjectMembership> ProjectMemberships { get; set; } = null!;

    public DbSet<Activity> Activities { get; set; } = null!;

    public DbSet<Entry> Entries { get; set; } = null!;

    public DbSet<TimeSheet> TimeSheets { get; set; } = null!;

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

public class User : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? DisplayName { get; set; }

    public string SSN { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class Project : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public List<Activity> Activities { get; set; } = new List<Activity>();

    public List<Entry> Entries { get; set; } = new List<Entry>();

    public List<ProjectMembership> Memberships { get; set; } = new List<ProjectMembership>();

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class ProjectMembership : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public Project Project { get; set; } = null!;

    public User User { get; set; } = null!;

    public DateTime? From { get; set; }

    public DateTime? Thru { get; set; }

    /// <summary>
    /// Expected hours per week / timesheet
    /// </summary>
    public double? ExpectedHoursWeekly { get; set; }

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class Activity : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Project Project { get; set; } = null!;

    public List<Entry> Entries { get; set; } = new List<Entry>();

    /// <summary>
    /// Minimum hours per day / entry
    /// </summary>
    public double? MinHoursPerDay { get; set; }

    /// <summary>
    /// Maximum hours per day / entry
    /// </summary>
    public double? MaxHoursPerDay { get; set; }

    /// <summary>
    /// Hourly rate. Positive value = Revenue and Negative value = Cost
    /// </summary>
    public decimal? HourlyRate { get; set; }

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class Entry : AuditableEntity
{
    public string Id { get; set; } = null!;

    public User User { get; set; } = null!;

    public Project Project { get; set; } = null!;

    public Activity Activity { get; set; } = null!;

    public TimeSheet TimeSheet { get; set; } = null!;

    public TimeSheetActivity TimeSheetActivity { get; set; } = null!;

    public DateOnly Date { get; set; }

    public double? Hours { get; set; }

    public string? Description { get; set; }
}

public class TimeSheet : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public User? User { get; set; }

    public int Year { get; set; }

    public int Week { get; set; }

    public TimeSheetStatus Status { get; set; }

    public List<TimeSheetActivity> Activities { get; set; } = new List<TimeSheetActivity>();

    public List<Entry> Entries { get; set; } = new List<Entry>();

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public enum TimeSheetStatus
{
    Open,
    Closed,
    Approved,
    Disapproved
}

public class TimeSheetActivity : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public TimeSheet TimeSheet { get; set; } = null!;

    public Project Project { get; set; } = null!;

    public Activity Activity { get; set; } = null!;

    public List<Entry> Entries { get; set; } = new List<Entry>();

    // public decimal? HourlyRate { get; set; }

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}