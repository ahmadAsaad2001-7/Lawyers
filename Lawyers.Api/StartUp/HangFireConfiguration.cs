using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;

namespace Lawyers.Api.StartUp;

public static class HangFireConfiguration
{
    public static void HangFireConfig(this WebApplicationBuilder builder)
    {
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddHangfireServer();
    }

    public static void HangFireBuild(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });
    }

    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true
                   && httpContext.User.IsInRole("Admin"); // adjust to your role scheme
        }
    }
    }
    
