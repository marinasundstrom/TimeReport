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

        context.Users.AddRange(new User[] {
                new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "Alice",
                    LastName = "McDonald",
                    Created = DateTime.Now
                },
                new User()
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = "Robert",
                    LastName = "Johnson",
                    DisplayName = "Bob Johnson",
                    Created = DateTime.Now
                }
            });

        await context.SaveChangesAsync();

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

        await context.SaveChangesAsync();
    }
}