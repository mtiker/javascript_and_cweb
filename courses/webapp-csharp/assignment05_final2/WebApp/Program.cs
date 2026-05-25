using Modules.Gyms.Api;
using Modules.Maintenance.Api;
using Modules.Memberships.Api;
using Modules.Training.Api;
using Modules.Users.Api;
using WebApp.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();
builder.Services.AddAppDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAppIdentity(builder.Configuration, builder.Environment);
builder.Services.AddAppServices();
builder.Services.AddAppLocalization();
builder.Services.AddAppControllers();
builder.Services.AddAppForwardedHeaders();
builder.Services.AddAppCors(builder.Configuration, builder.Environment);
builder.Services.AddAppApiVersioning();
builder.Services.AddAppSwagger();

// Final2 modular monolith composition (Phase 2 shells — see docs/final2-module-map.md).
builder.Services.AddUsersModule(builder.Configuration);
builder.Services.AddGymsModule(builder.Configuration);
builder.Services.AddMembershipsModule(builder.Configuration);
builder.Services.AddTrainingModule(builder.Configuration);
builder.Services.AddMaintenanceModule(builder.Configuration);

var app = builder.Build();

await app.SetupAppDataAsync();
app.UseAppSwagger();
app.UseAppPipeline();
app.MapAppEndpoints();

app.Run();

public partial class Program
{
}
