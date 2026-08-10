using Lawyers.Api.Hubs;
using Lawyers.Api.StartUp;      // Fixed Api -> API
using Lawyers.Api.Hubs;
using Lawyers.Api.StartUp; // Fixed Api -> API
using Lawyers.InfraStructure.Data; // Matched your actual folder casing (InfraStructure)
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Load all dependencies
builder.AddDependencies();
builder.HangFireConfig();
builder.AuthConfigure();
builder.CorsConfigure();

// 2. Database Setup (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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