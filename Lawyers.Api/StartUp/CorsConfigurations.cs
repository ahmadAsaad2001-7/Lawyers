namespace Lawyers.Api.StartUp;

public static class CorsConfigurations
{
    public static WebApplicationBuilder CorsConfigure(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("NuxtPolicy", policy =>
            {
                policy.WithOrigins(
                        "http://localhost:3000",
                        "http://127.0.0.1:3000",
                        "https://lawyers-tau.vercel.app")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials(); // Critical for SignalR WebRTC signaling
            });
        });

        return builder;
    }
}