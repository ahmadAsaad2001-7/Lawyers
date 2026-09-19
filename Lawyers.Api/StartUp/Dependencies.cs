using System.Text.Json.Serialization;
using FluentValidation;
using Lawyers.Api.Services;
using Lawyers.Application.Interfaces;
using Lawyers.Infrastructure.Data.Repositories;
using Lawyers.InfraStructure.Helpers;
using Lawyers.Infrastructure.Services;
using Lawyers.InfraStructure.Services; // Ensure this matches your actual namespace casing

namespace Lawyers.Api.StartUp;

public static class Dependencies
{
    public static void AddDependencies(this WebApplicationBuilder builder) // Fixed typo in method name
    {
        var configuration = builder.Configuration;
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
        builder.Services.AddControllers();
        builder.Services.AddSignalR();
        builder.Services.AddOpenApi(); // Note: Requires .NET 9/10
        builder.Services.AddHttpContextAccessor();
        
        // 1. Database & Repositories
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // 2. Application Services
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        
        // ✅ ADDED THIS: Register the Email Service so AuthService can use it!
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();

        // 3. MediatR & Validation (Application handlers + Infrastructure event subscribers)
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(IUnitOfWork).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(Lawyers.Infrastructure.Notifications.SignalR.ConsultationConfirmedSignalRHandler).Assembly);
        });
        builder.Services.AddValidatorsFromAssembly(typeof(IUnitOfWork).Assembly);

        // 4. Notifications
        builder.Services.AddScoped<INotificationService, SignalRNotificationService>();
        
        // 5. Payment Gateway
        builder.Services.Configure<KashierOptions>(configuration.GetSection("KashierSettings"));

        builder.Services.AddHttpClient<IPaymentService, KashierPaymentService>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
    }
}
