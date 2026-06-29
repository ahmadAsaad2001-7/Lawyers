using Lawyers.Api.StartUp;
using Lawyers.Infrastructure.Data;
using Lawyers.InfraStructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Load all dependencies (including Controllers and OpenApi)
builder.AddDependecies();
builder.HangFireConfig();
builder.AuthConfigure();
// 2. Database Setup
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 3. Configure Middleware & Scalar UI
app.OpenApiConfiguration();

app.UseHttpsRedirection();

// 4. REQUIRED: This maps the attribute routes like [Route("api/[controller]")]
app.MapControllers(); 


app.Run();