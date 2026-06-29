using Hangfire;

namespace Lawyers.Api.StartUp;

public static class HangFireConfiguration
{
    public static void HangFireConfig(this WebApplicationBuilder builder)
    {
        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
        builder.Services.AddHangfireServer();
    }

    public static void HangFireBuild(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire");    
        
    }
    
}