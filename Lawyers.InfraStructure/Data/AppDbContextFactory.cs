using Lawyers.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Lawyers.InfraStructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var apiProjectPath = FindApiProjectPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiProjectPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        optionsBuilder.UseNpgsql(connectionString);
        return new AppDbContext(optionsBuilder.Options, new DummyCurrentUserService());
    }

    private static string FindApiProjectPath()
    {
        var searchRoots = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var root in searchRoots)
        {
            var directory = new DirectoryInfo(root);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "Lawyers.Api");
                var appSettingsPath = Path.Combine(candidate, "appsettings.json");

                if (File.Exists(appSettingsPath))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException("Could not find Lawyers.Api/appsettings.json for design-time DbContext creation.");
    }
}

internal class DummyCurrentUserService : ICurrentUserService
{
    public int? UserId => null;
    public bool IsAuthenticated => false;
    public string? Email => null;
}
