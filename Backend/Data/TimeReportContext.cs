﻿using System;

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

        modelBuilder.Entity<User>().HasQueryFilter(i => i.Deleted == null);

        modelBuilder.Entity<Project>().HasQueryFilter(i => i.Deleted == null);
        modelBuilder.Entity<ProjectMembership>().HasQueryFilter(i => i.Deleted == null);
        modelBuilder.Entity<Activity>().HasQueryFilter(i => i.Deleted == null);

        modelBuilder.Entity<Entry>();

        modelBuilder.Entity<TimeSheet>().HasQueryFilter(i => i.Deleted == null);
        modelBuilder.Entity<TimeSheetActivity>().HasQueryFilter(i => i.Deleted == null);

        modelBuilder.Entity<Entry>()
            .Property(x => x.Date)
            .HasConversion(x => x.ToDateTime(TimeOnly.Parse("01:00")), x => DateOnly.FromDateTime(x));
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

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class Project : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public List<Activity> Activities { get; set; } = new List<Activity>();

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

    public double? MinHours { get; set; }

    public double? MaxHours { get; set; }

    public decimal? HourlyRate { get; set; }

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class Entry : AuditableEntity
{
    public string Id { get; set; } = null!;

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

    //public double? MinHours { get; set; }

    //public double? MaxHours { get; set; }

    //public User User { get; set; } = null!;

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

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}