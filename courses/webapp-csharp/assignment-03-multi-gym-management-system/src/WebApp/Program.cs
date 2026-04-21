using WebApp.Setup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.SeedAppAsync();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAppPipeline();

app.Run();

public partial class Program
{
}
