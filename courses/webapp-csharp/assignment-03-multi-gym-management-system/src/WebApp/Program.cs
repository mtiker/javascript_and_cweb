using WebApp.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();
builder.Services.AddAppDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAppIdentity(builder.Configuration);
builder.Services.AddAppServices();
builder.Services.AddAppLocalization();
builder.Services.AddAppControllers();
builder.Services.AddAppCors(builder.Configuration);
builder.Services.AddAppApiVersioning();
builder.Services.AddAppSwagger();

var app = builder.Build();

await app.SetupAppDataAsync();
app.UseAppSwagger();
app.UseAppPipeline();
app.MapAppEndpoints();

app.Run();

public partial class Program
{
}
