using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using GooseWebsite.Api.Modules.Media.Services;

namespace GooseWebsite.Api.Modules.Media;

/// <summary>
/// Media module: administrator-only image uploads, and public serving of the uploaded files under <c>/uploads</c>.
/// The folder is <c>Media:Directory</c>, or <c>uploaded-images</c> beside the application when not set.
/// </summary>
public static class MediaModule
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.Configure<MediaOptions>(options =>
            options.Directory = configuration["Media:Directory"] ?? Path.Combine(environment.ContentRootPath, "uploaded-images"));
        services.AddScoped<IImageService, ImageService>();
        return services;
    }

    /// <summary>Serves the uploaded files. Only files this module wrote are there, and browsers are told not to guess their type.</summary>
    public static IApplicationBuilder UseUploadedImages(this WebApplication app)
    {
        var directory = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<MediaOptions>>().Value.Directory;
        Directory.CreateDirectory(directory);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(directory),
            RequestPath = "/uploads",
            ServeUnknownFileTypes = false,
            OnPrepareResponse = context => context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
        });
        return app;
    }
}
