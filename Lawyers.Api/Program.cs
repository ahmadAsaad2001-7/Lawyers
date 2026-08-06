using Lawyers.Api.StartUp;
using Lawyers.Api.Hubs;
using Lawyers.Infrastructure.Data;
using Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Load all dependencies (including Controllers and OpenApi)
builder.AddDependencies();
builder.HangFireConfig();
builder.AuthConfigure();
builder.CorsConfigure();
// 2. Database Setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.SeedRolesAsync();
// 3. Configure Middleware & Scalar UI
app.OpenApiConfiguration();

app.UseHttpsRedirection();
app.UseCors("NuxtPolicy");
app.UseAuthentication();
app.UseAuthorization();

// 4. REQUIRED: This maps the attribute routes like [Route("api/[controller]")]
app.MapControllers();
app.MapHub<ConsultationHub>("/hubs/consultations", options =>
{
    options.CloseOnAuthenticationExpiration = true;
});


app.Run();
