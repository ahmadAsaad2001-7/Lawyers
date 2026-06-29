using FluentValidation;
using Lawyers.Application.Interfaces;
using Lawyers.Infrastructure.Data.Repositories;
using Lawyers.InfraStructure.Helpers;
using Lawyers.Infrastructure.Services;
using Lawyers.InfraStructure.Services;

namespace Lawyers.Api.StartUp;

public static class Dependencies
{
    private static IConfiguration configuration;

    public static void AddDependecies(this WebApplicationBuilder builder)
    {
        
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Lawyers.Application.DTOs.GetLawyersQuery).Assembly));
        builder.Services.AddValidatorsFromAssembly(typeof(Lawyers.Application.DTOs.GetLawyersQuery).Assembly);
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ILawyerService, LawyerService>();
        builder.Services.Configure<PaymobOptions>(configuration.GetSection(PaymobOptions.SectionName));
        builder.Services.AddHttpClient<IPaymentService, KashierPaymentService>(client =>
        {
            client.BaseAddress = new Uri("https://api.kashier.io/"); 
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });    
    }
}