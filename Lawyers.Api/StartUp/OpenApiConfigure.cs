using Scalar.AspNetCore;

namespace Lawyers.Api.StartUp;

public static  class OpenApiConfigure
{
    public static void OpenApiConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            { options.Title = "Scalar API";
                options.Theme=ScalarTheme.Moon;
                options.Layout = ScalarLayout.Modern;
                options.HideClientButton = true;

            });
        }
    }
}