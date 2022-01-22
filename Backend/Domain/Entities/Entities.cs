using System;

using Microsoft.EntityFrameworkCore;

using TimeReport.Domain.Common.Interfaces;
using TimeReport.Services;

namespace TimeReport.Domain.Entities;

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

    public List<Expense> Expenses { get; set; } = new List<Expense>();

    public List<Activity> Activities { get; set; } = new List<Activity>();

    public List<Entry> Entries { get; set; } = new List<Entry>();

    public List<ProjectMembership> Memberships { get; set; } = new List<ProjectMembership>();

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public class Expense : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public Project Project { get; set; } = null!;

    public ExpenseType Type { get; set; }

    public DateOnly Date { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public string? Attachment { get; set; }

    public DateTime? Deleted { get; set; }
    public string? DeletedBy { get; set; }
}

public enum ExpenseType
{
    Purchase = 1
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

    public MonthEntryGroup? MonthGroup { get; set; }

    public DateOnly Date { get; set; }

    public double? Hours { get; set; }

    public string? Description { get; set; }

    public EntryStatus Status { get; set; } = EntryStatus.Unlocked;
}

public class MonthEntryGroup : AuditableEntity
{
    public string Id { get; set; } = null!;

    public User User { get; set; } = null!;

    public int Year { get; set; }

    public int Month { get; set; }

    public List<Entry> Entries { get; set; } = new List<Entry>();

    public EntryStatus Status { get; set; } = EntryStatus.Unlocked;
}

public enum EntryStatus
{
    Unlocked,
    Locked
}

public class TimeSheet : AuditableEntity, ISoftDelete
{
    public string Id { get; set; } = null!;

    public User? User { get; set; }

    public int Year { get; set; }

    public int Week { get; set; }

    public DateTime From { get; set; }

    public DateTime To { get; set; }

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