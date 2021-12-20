using System.Globalization;

using TimeReport;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

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
            http.BaseAddress = new Uri(UriString);
        })
        .AddTypedClient<TimeReport.Client.ITimeSheetsClient>((http, sp) => new TimeReport.Client.TimeSheetsClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IProjectsClient), (sp, http) =>
        {
            http.BaseAddress = new Uri(UriString);
        })
        .AddTypedClient<TimeReport.Client.IProjectsClient>((http, sp) => new TimeReport.Client.ProjectsClient(http));

builder.Services.AddHttpClient(nameof(TimeReport.Client.IActivitiesClient), (sp, http) =>
        {
            http.BaseAddress = new Uri(UriString);
        })
        .AddTypedClient<TimeReport.Client.IActivitiesClient>((http, sp) => new TimeReport.Client.ActivitiesClient(http));


await builder.Build().RunAsync();