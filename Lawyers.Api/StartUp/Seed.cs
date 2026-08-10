using Lawyers.Domain.Entities;
using Lawyers.Domain.Entities.Enums;
using Lawyers.Domain.ValueObjects;
using Lawyers.InfraStructure.Data; // Ensure this matches your DbContext namespace
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lawyers.Api.StartUp;

public static class Seed
{
    public static async Task<WebApplication> SeedDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = services.GetRequiredService<UserManager<User>>(); // ✅ Corrected to 'User'
            var dbContext = services.GetRequiredService<AppDbContext>();

            // 1. Seed Roles
            string[] roleNames = Enum.GetNames<Roles>();
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            // 2. Seed Admin
            await SeedAdmin(userManager);

            // 3. Seed 11 Lawyers
            await SeedLawyers(userManager, dbContext);

            // 4. Seed 2 Clients
            await SeedClients(userManager, dbContext);
            
            // Save all profiles to the database
            await dbContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(e, "An error occurred while seeding the database.");
            throw;
        }
        return app;
    }

    private static async Task SeedAdmin(UserManager<User> userManager)
    {
        string adminEmail = "admin@lawyers.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                Role = Roles.Admin, // ✅ Corrected: Uses the Enum directly
                ProfileImageUrl = "https://i.pravatar.cc/150?u=admin"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin.ToString());
            }
        }
    }

    private static async Task SeedLawyers(UserManager<User> userManager, AppDbContext dbContext)
    {
        var lawyersData = new[]
        {
            new { Email = "ahmed.hassan@lawyers.com", Name = "Ahmed Hassan", Bio = "Expert in corporate mergers and acquisitions.", Rate = 150m, City = "Cairo", State = "Cairo", Spec = "Corporate", Firm = "Hassan & Partners", Bar = "EG10001" },
            new { Email = "mona.ali@lawyers.com", Name = "Mona Ali", Bio = "Specialized in family law and custody cases.", Rate = 120m, City = "Alexandria", State = "Alexandria", Spec = "Family", Firm = "Ali Legal Consults", Bar = "EG10002" },
            new { Email = "omar.farouk@lawyers.com", Name = "Omar Farouk", Bio = "Aggressive criminal defense attorney.", Rate = 200m, City = "Giza", State = "Giza", Spec = "Criminal", Firm = "Farouk Defense", Bar = "EG10003" },
            new { Email = "layla.mahmoud@lawyers.com", Name = "Layla Mahmoud", Bio = "Real estate and property registration expert.", Rate = 100m, City = "Mansoura", State = "Dakahlia", Spec = "Real Estate", Firm = "Mahmoud Realty Law", Bar = "EG10004" },
            new { Email = "youssef.ibrahim@lawyers.com", Name = "Youssef Ibrahim", Bio = "Intellectual property and trademark registration.", Rate = 180m, City = "Luxor", State = "Luxor", Spec = "Intellectual Property", Firm = "Ibrahim IP Group", Bar = "EG10005" },
            new { Email = "salma.elsayed@lawyers.com", Name = "Salma El-Sayed", Bio = "Labor law and corporate dispute resolution.", Rate = 130m, City = "Aswan", State = "Aswan", Spec = "Labor", Firm = "El-Sayed Advocates", Bar = "EG10006" },
            new { Email = "tarek.nabil@lawyers.com", Name = "Tarek Nabil", Bio = "Commercial contracts and international trade.", Rate = 160m, City = "Tanta", State = "Gharbia", Spec = "Commercial", Firm = "Nabil Trade Law", Bar = "EG10007" },
            new { Email = "nourhan.adel@lawyers.com", Name = "Nourhan Adel", Bio = "Tax evasion defense and corporate structuring.", Rate = 170m, City = "Ismailia", State = "Ismailia", Spec = "Tax", Firm = "Adel Tax & Legal", Bar = "EG10008" },
            new { Email = "khaled.mostafa@lawyers.com", Name = "Khaled Mostafa", Bio = "Maritime law and shipping contracts.", Rate = 190m, City = "Port Said", State = "Port Said", Spec = "Maritime", Firm = "Mostafa Maritime", Bar = "EG10009" },
            new { Email = "dina.samir@lawyers.com", Name = "Dina Samir", Bio = "Cybercrime and tech startup compliance.", Rate = 210m, City = "Suez", State = "Suez", Spec = "Cyber Law", Firm = "Samir Tech Law", Bar = "EG10010" },
            new { Email = "hany.refaat@lawyers.com", Name = "Hany Refaat", Bio = "Constitutional law and human rights.", Rate = 140m, City = "Asyut", State = "Asyut", Spec = "Constitutional", Firm = "Refaat Rights Center", Bar = "EG10011" }
        };

        foreach (var l in lawyersData)
        {
            if (await userManager.FindByEmailAsync(l.Email) == null)
            {
                var user = new User
                {
                    UserName = l.Email,
                    Email = l.Email,
                    EmailConfirmed = true,
                    Role = Roles.Lawyer, // ✅ Corrected: Uses the Enum directly
                    ProfileImageUrl = $"https://i.pravatar.cc/150?u={l.Email}"
                };

                var result = await userManager.CreateAsync(user, "Lawyer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.Lawyer.ToString());

                    var profile = new LawyerProfile
                    {
                        UserId = user.Id,
                        FullName = l.Name,
                        Bio = l.Bio,
                        HourlyRate = l.Rate,
                        
                        // ✅ Corrected: Address is instantiated as a Value Object
                        Address = new Address 
                        { 
                            Street = "123 Legal St", 
                            City = l.City, 
                            State = l.State, 
                            Country = "Egypt", 
                            PostalCode = "11511" 
                        },
                        
                        ProfileImageUrl = user.ProfileImageUrl,
                        BarLicenseNumber = l.Bar,
                        IsVerified = true,
                        Specialization = l.Spec,
                        AverageRating = 4.8m,
                        LawFirmName = l.Firm,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    // Using Set<T>() ensures it works regardless of your DbSet property names
                    dbContext.Set<LawyerProfile>().Add(profile); 
                }
            }
        }
    }

    private static async Task SeedClients(UserManager<User> userManager, AppDbContext dbContext)
    {
        var clientsData = new[]
        {
            new { Email = "mahmoud.abdallah@client.com", Name = "Mahmoud Abdallah", Phone = "01022223333" },
            new { Email = "fatima.zahra@client.com", Name = "Fatima Zahra", Phone = "01044445555" }
        };

        foreach (var c in clientsData)
        {
            if (await userManager.FindByEmailAsync(c.Email) == null)
            {
                var user = new User
                {
                    UserName = c.Email,
                    Email = c.Email,
                    EmailConfirmed = true,
                    Role = Roles.Client, // ✅ Corrected: Uses the Enum directly
                    ProfileImageUrl = $"https://i.pravatar.cc/150?u={c.Email}"
                };

                var result = await userManager.CreateAsync(user, "Client@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Roles.Client.ToString());

                    var profile = new ClientProfile
                    {
                        UserId = user.Id,
                        FullName = c.Name,
                        PhoneNumber = c.Phone,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    dbContext.Set<ClientProfile>().Add(profile);
                }
            }
        }
    }
}