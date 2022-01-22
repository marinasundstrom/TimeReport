using System.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using TimeReport;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

string UriString = $"https://localhost:8080/";

builder.Services.AddHttpClient(nameof(TimeReport.Client.ITimeSheetsClient), (sp, http) =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            http.BaseAddress = new Uri($"{navigationManager.BaseUri}api/");
        })
        .AddTypedClient<TimeReport.Client.ITimeSheetsClient>((http, sp) => new TimeReport.Client.TimeSheetsClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IProjectsClient), (sp, http) =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            http.BaseAddress = new Uri($"{navigationManager.BaseUri}api/");
        })
        .AddTypedClient<TimeReport.Client.IProjectsClient>((http, sp) => new TimeReport.Client.ProjectsClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IActivitiesClient), (sp, http) =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            http.BaseAddress = new Uri($"{navigationManager.BaseUri}api/");
        })
        .AddTypedClient<TimeReport.Client.IActivitiesClient>((http, sp) => new TimeReport.Client.ActivitiesClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IReportsClient), (sp, http) =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            http.BaseAddress = new Uri($"{navigationManager.BaseUri}api/");
        })
        .AddTypedClient<TimeReport.Client.IReportsClient>((http, sp) => new TimeReport.Client.ReportsClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IUsersClient), (sp, http) =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            http.BaseAddress = new Uri($"{navigationManager.BaseUri}api/");
        })
        .AddTypedClient<TimeReport.Client.IUsersClient>((http, sp) => new TimeReport.Client.UsersClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IExpensesClient), (sp, http) =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            http.BaseAddress = new Uri($"{navigationManager.BaseUri}api/");
        })
        .AddTypedClient<TimeReport.Client.IExpensesClient>((http, sp) => new TimeReport.Client.ExpensesClient(http));


await builder.Build().RunAsync();