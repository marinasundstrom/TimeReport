using System.Globalization;

using Microsoft.EntityFrameworkCore;

using TimeReport.Data;

static class Seed 
{
    public static async Task SeedAsync(this WebApplication app) 
    {
        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<TimeReportContext>();
        
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        if (!context.Users.Any())
        {
            context.Users.AddRange(new User[] {
                new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "Test",
                    LastName = "Testsson",
                    Created = DateTime.Now
                },
                new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "Admin",
                    LastName = String.Empty,
                    DisplayName = "Admin",
                    Created = DateTime.Now
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

            var activity3 = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Övrigt",
                MinHours = null,
                MaxHours = null
            };

            project.Activities.Add(activity3);

            var project2 = new Project
            {
                Id = Guid.NewGuid().ToString(),
                Name = "ACME"
            };

            context.Projects.Add(project2);

            var activity2 = new Activity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Konsulttid",
                MinHours = null,
                MaxHours = null
            };

            project2.Activities.Add(activity2);

            var timeSheet = new TimeSheet
            {
                Id = Guid.NewGuid().ToString(),
                User = await context.Users.FirstAsync(),
                Year = DateTime.Now.Year,
                Week = ISOWeek.GetWeekOfYear(DateTime.Now)
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
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
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