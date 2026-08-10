namespace Lawyers.Api.StartUp;

public static class CorsConfigurations
{
    public static WebApplicationBuilder CorsConfigure(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("NuxtPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:3000") // Your Nuxt development server
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Critical for SignalR WebRTC signaling
            });
        });

        return builder;
    }
}