using System.Text;
using Lawyers.Domain.Entities;
using Lawyers.InfraStructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Lawyers.Api.StartUp;

public static class AuthConfiguration
{
    public static void AuthConfigure( this WebApplicationBuilder builder)
    {
        
        // 1. Configure JWT Settings (Read from appsettings.json)
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));

// 2. Add Identity
        builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 7;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;  
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

// 3. Add JWT Authentication
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in Production!
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero // Remove the default 5-minute delay
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs/consultations"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        
        builder.Services.AddAuthorization();
            
        
    }
}
