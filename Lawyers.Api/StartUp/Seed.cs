using Lawyers.Domain.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace Lawyers.Api.StartUp;

public static class Seed
{
    public static async Task<WebApplication> SeedRolesAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var RoleManager=services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            string[] RoleNames = Enum.GetNames<Roles>();
            foreach (var e in RoleNames)
            {
                var RoleExist=await RoleManager.RoleExistsAsync(e);
                if (!RoleExist)
                    await RoleManager.CreateAsync(new IdentityRole<int>(e));
                
            }
        }catch(Exception e)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(e,"SeedRoles");
            throw;
        }
        return app;
    }
}