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

        var project = new Project
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Internt",
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        context.Projects.Add(project);

        var activity = new Activity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Bänken",
            MinHours = null,
            MaxHours = null,
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        project.Activities.Add(activity);

        var activity3 = new Activity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Övrigt",
            MinHours = null,
            MaxHours = null,
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        project.Activities.Add(activity3);

        var project2 = new Project
        {
            Id = Guid.NewGuid().ToString(),
            Name = "ACME",
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        context.Projects.Add(project2);

        var activity2 = new Activity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Konsulttid",
            MinHours = null,
            MaxHours = null,
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        project2.Activities.Add(activity2);

        await context.SaveChangesAsync();

        var user1 = new User()
        {
            Id = Guid.NewGuid().ToString(),
            SSN = "sdfsdfsd",
            FirstName = "Alice",
            LastName = "McDonald",
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        var user2 = new User()
        {
            Id = Guid.NewGuid().ToString(),
            SSN = "sfsdfsdf",
            FirstName = "Robert",
            LastName = "Johnson",
            DisplayName = "Bob Johnson",
            Created = DateTime.Now,
            CreatedBy = "N/A"
        };

        context.Users.AddRange(new User[] {
                user1, user2
            });

        await context.SaveChangesAsync();

        project.Memberships.Add(new ProjectMembership()
        {
            Id = Guid.NewGuid().ToString(),
            User = user1,
            Created = DateTime.Now,
            CreatedBy = "N/A"
        });

        project.Memberships.Add(new ProjectMembership()
        {
            Id = Guid.NewGuid().ToString(),
            User = user1,
            Created = DateTime.Now,
            CreatedBy = "N/A"
        });

        project2.Memberships.Add(new ProjectMembership()
        {
            Id = Guid.NewGuid().ToString(),
            User = user2,
            Created = DateTime.Now,
            CreatedBy = "N/A"
        });

        await context.SaveChangesAsync();
    }
}