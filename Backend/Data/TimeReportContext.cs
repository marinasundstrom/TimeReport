using System;

using Microsoft.EntityFrameworkCore;

namespace TimeReport.Data;

public class TimeReportContext : DbContext
{
    public TimeReportContext(DbContextOptions<TimeReportContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasQueryFilter(i => i.Deleted == null);

        modelBuilder.Entity<Entry>()
            .Property(x => x.Date)
            .HasConversion(x => x.ToDateTime(TimeOnly.Parse("01:00")), x => DateOnly.FromDateTime(x));
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Project> Projects { get; set; } = null!;

    public DbSet<Activity> Activities { get; set; } = null!;

    public DbSet<Entry> Entries { get; set; } = null!;

    public DbSet<TimeSheet> TimeSheets { get; set; } = null!;
}

public class User
{
    public User()
    {

    }

    public string Id { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? DisplayName { get; set; }

    public DateTime Created { get; set; }
    public DateTime? Modified { get; set; }
    public DateTime? Deleted { get; set; }
}

public class Project
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public List<Activity> Activities { get; set; } = new List<Activity>();
}

public class Activity
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Project Project { get; set; } = null!;

    public List<Entry> Entries { get; set; } = new List<Entry>();

    public double? MinHours { get; set; }

    public double? MaxHours { get; set; }

    public decimal? HourlyRate { get; set; }
}

public class Entry
{
    public string Id { get; set; } = null!;

    public Project Project { get; set; } = null!;

    public Activity Activity { get; set; } = null!;

    public TimeSheet TimeSheet { get; set; } = null!;

    public DateOnly Date { get; set; }

    public double? Hours { get; set; }

    public string? Description { get; set; }
}

public class TimeSheet
{
    public string Id { get; set; } = null!;

    public User? User { get; set; }

    public int Year { get; set; }

    public int Week { get; set; }

    public TimeSheetStatus Status { get; set; }

    //public double? MinHours { get; set; }

    //public double? MaxHours { get; set; }

    //public User User { get; set; } = null!;

    public List<Entry> Entries { get; set; } = new List<Entry>();
}

public enum TimeSheetStatus
{
    Open,
    Closed,
    Approved,
    Disapproved
}