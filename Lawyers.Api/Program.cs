using Lawyers.Api.Hubs;
using Lawyers.Api.StartUp;      // Fixed Api -> API
using Lawyers.Api.Hubs;
using Lawyers.Api.StartUp; // Fixed Api -> API
using Lawyers.InfraStructure.Data; // Matched your actual folder casing (InfraStructure)
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString) ||
    connectionString.Contains("YOUR_SQL_SERVER", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException(
        "SQL connection string is missing. Publish appsettings.Secrets.json with the site, or set ConnectionStrings__DefaultConnection on the host.");
}

// 1. Load all dependencies
builder.AddDependencies();
builder.HangFireConfig();
builder.AuthConfigure();
builder.CorsConfigure();

// 2. Database Setup (SQL Server)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
var app = builder.Build();

// 3. Seed Roles (Awaited to avoid startup race conditions)
await app.SeedDataAsync(); 
// 4. Configure Middleware & UI
app.OpenApiConfiguration();

app.UseHttpsRedirection();
app.UseCors("NuxtPolicy");
app.UseAuthentication();
app.UseAuthorization();

// 5. Map Endpoints & SignalR
app.MapControllers();
app.MapHub<ConsultationHub>("/hubs/consultations", options =>
{
    options.CloseOnAuthenticationExpiration = true;
});

app.Run();