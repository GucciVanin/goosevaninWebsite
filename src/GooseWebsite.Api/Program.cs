using GooseWebsite.Api.Modules.Accounts;
using GooseWebsite.Api.Modules.Blog;
using GooseWebsite.Api.Modules.Contact;
using GooseWebsite.Api.Shared.Email;
using GooseWebsite.Api.Shared.Hosting;
using GooseWebsite.Api.Shared.Modules;
using GooseWebsite.Api.Shared.Persistence;

// Composition root: shared services first, then one line per feature module.
// To add a feature, create Modules/<Name>/<Name>Module.cs and register it below.
var builder = WebApplication.CreateBuilder(args);

// AddControllersWithViews (not AddControllers) registers the filter behind [ValidateAntiForgeryToken].
builder.Services.AddControllersWithViews();
builder.Services.AddTrustedProxyHeaders(builder.Configuration);
builder.Services.AddSharedPersistence(builder.Configuration, builder.Environment);
builder.Services.AddSmtpEmail(builder.Configuration);
builder.Services.AddMailerSendEmail(builder.Configuration);

builder.Services.AddAccountsModule(builder.Configuration, builder.Environment);
builder.Services.AddBlogModule();
builder.Services.AddContactModule(builder.Configuration);

var app = builder.Build();

await app.InitializeModulesAsync();
if (await app.TryRunModuleCommandAsync(args))
{
    return;
}

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

// Exposes the generated entry point so integration tests can host the application.
public partial class Program;
