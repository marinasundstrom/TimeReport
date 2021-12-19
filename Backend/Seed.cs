using System.Globalization;

using TimeReport.Data;

static class Seed 
{
    public static async Task SeedAsync(this WebApplication app) 
    {
        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<TimeReportContext>();
        
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        if (!context.Items.Any())
        {
            context.Items.AddRange(new Item[] {
                new Item(Guid.NewGuid().ToString(), "Hat", "Green hat")
                {
                    CreatedAt = DateTime.Now
                }
            });

            await context.SaveChangesAsync();
        }

        if (!context.TimeSheets.Any())
        {
            var project = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Internt"
            };

            context.Projects.Add(project);

            var activity = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Bänken",
                MinHours = null,
                MaxHours = null
            };

            project.Activities.Add(activity);

            var project2 = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Axis"
            };

            context.Projects.Add(project2);

            var activity2 = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Konsultuppdrag",
                MinHours = null,
                MaxHours = null
            };

            project2.Activities.Add(activity2);

            var timeSheet = new TimeSheet
            {
                Id = Guid.NewGuid().ToString(),
                Year = DateTime.Now.Year,
                Week = ISOWeek.GetWeekOfYear(DateTime.Now),
            };

            timeSheet.Entries.AddRange(new[] {
                new Entry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = DateOnly.FromDateTime(DateTime.Now),
                    Project = project,
                    Activity = activity,
                    Hours = 5,
                    Description = "I väntan på uppdrag"
                },
                new Entry
                {
                    Id = Guid.NewGuid().ToString(),
                    Date = DateOnly.FromDateTime(DateTime.Now),
                    Project = project2,
                    Activity = activity2,
                    Hours = 2,
                    Description = null
                }
            });

            context.TimeSheets.Add(timeSheet);

            await context.SaveChangesAsync();
        }
    }
}