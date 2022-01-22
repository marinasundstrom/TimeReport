using System.Globalization;

using Azure.Identity;
using Azure.Storage.Blobs;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;

using TimeReport;
using TimeReport.Infrastructure;
using TimeReport.Hubs;
using TimeReport.Services;
using TimeReport.Application.Common.Interfaces;
using TimeReport.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

var Configuration = builder.Configuration;

services
    .AddControllers()
    .AddNewtonsoftJson();

services.AddHttpContextAccessor();

services.AddMediatR(typeof(Program));

services.AddScoped<ICurrentUserService, CurrentUserService>();

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("sv-SE");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

services.AddSqlServer<TimeReportContext>(Configuration.GetConnectionString("mssql"));
services.AddScoped<ITimeReportContext>(sp => sp.GetRequiredService<TimeReportContext>());

services.AddEndpointsApiExplorer();

// Register the Swagger services
services.AddOpenApiDocument(config =>
{
    config.Title = "Web API";
    config.Version = "v1";
});

services.AddAzureClients(builder =>
{
    // Add a KeyVault client
    //builder.AddSecretClient(keyVaultUrl);

    // Add a Storage account client
    builder.AddBlobServiceClient(Configuration.GetConnectionString("Azure:Storage"))
            .WithVersion(BlobClientOptions.ServiceVersion.V2019_07_07);

    // Use DefaultAzureCredential by default
    builder.UseCredential(new DefaultAzureCredential());
});

services.AddSignalR();

services.AddMediatR(typeof(Program));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();

    app.UseOpenApi();
    app.UseSwaggerUi3(c => c.DocumentTitle = "Web API v1");
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

//app.MapApplicationRequests();

app.MapGet("/info", () =>
{
    return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
})
.WithDisplayName("GetInfo")
.WithName("GetInfo")
.WithTags("Info")
.Produces<string>();
 
//await app.SeedAsync();

app.MapControllers();
app.MapHub<ItemsHub>("/hubs/items");
app.MapFallbackToFile("index.html"); 

app.Run(); 