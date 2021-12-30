using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TimeReport.Services;

namespace TimeReport.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t => t.IsTemporal());
        builder.HasQueryFilter(i => i.Deleted == null);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects", t => t.IsTemporal());
        builder.HasQueryFilter(i => i.Deleted == null);
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses", t => t.IsTemporal());

        builder.Property(x => x.Date)
            .HasConversion(x => x.ToDateTime(TimeOnly.Parse("01:00")), x => DateOnly.FromDateTime(x));

        builder.HasQueryFilter(i => i.Deleted == null);
    }
}

public class ProjectMembershipConfiguration : IEntityTypeConfiguration<ProjectMembership>
{
    public void Configure(EntityTypeBuilder<ProjectMembership> builder)
    {
        builder.ToTable("ProjectMemberships", t => t.IsTemporal());
        builder.HasQueryFilter(i => i.Deleted == null);
    }
}

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities", t => t.IsTemporal());
        builder.HasQueryFilter(i => i.Deleted == null);
    }
}

public class EntryConfiguration : IEntityTypeConfiguration<Entry>
{
    public void Configure(EntityTypeBuilder<Entry> builder)
    {
        builder.ToTable("Entries", t => t.IsTemporal());

        builder.Property(x => x.Date)
                .HasConversion(x => x.ToDateTime(TimeOnly.Parse("01:00")), x => DateOnly.FromDateTime(x));
    }
}

public class MonthEntryGroupConfiguration : IEntityTypeConfiguration<MonthEntryGroup>
{
    public void Configure(EntityTypeBuilder<MonthEntryGroup> builder)
    {
        builder.ToTable("MonthEntryGroups", t => t.IsTemporal());
    }
}

public class TimeSheetConfiguration : IEntityTypeConfiguration<TimeSheet>
{
    public void Configure(EntityTypeBuilder<TimeSheet> builder)
    {
        builder.ToTable("TimeSheets", t => t.IsTemporal());
        builder.HasQueryFilter(i => i.Deleted == null);
    }
}

public class TimeSheetActivityConfiguration : IEntityTypeConfiguration<TimeSheetActivity>
{
    public void Configure(EntityTypeBuilder<TimeSheetActivity> builder)
    {
        builder.ToTable("TimeSheetActivities", t => t.IsTemporal());
        builder.HasQueryFilter(i => i.Deleted == null); 
    }
}