using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using WebApp.Data;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console(new CompactJsonFormatter()));

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.Configure<EmailSenderOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<DiscordNotifier>();
builder.Services.AddHostedService<ReminderBackgroundService>();

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var path = httpContext.Request.Path;
        var isAuthPath = path.StartsWithSegments("/Identity/Account/Login") || path.StartsWithSegments("/Identity/Account/Register");
        if (!isAuthPath)
        {
            return RateLimitPartition.GetNoLimiter("bypass");
        }

        var key = $"{httpContext.Connection.RemoteIpAddress}:{path}";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5)
        });
    });
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return ValueTask.CompletedTask;
    };
});

if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/keys"));
}

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Board");
    options.Conventions.AuthorizeFolder("/Applications");
    options.Conventions.AuthorizeFolder("/Companies");
    options.Conventions.AuthorizeFolder("/Notes");
    options.Conventions.AuthorizeFolder("/Folders");
    options.Conventions.AuthorizeFolder("/Schedules");
    options.Conventions.AuthorizeFolder("/Colleagues");
    options.Conventions.AuthorizeFolder("/Calendar");
    options.Conventions.AuthorizeFolder("/Training");
    options.Conventions.AuthorizeFolder("/Exercises");
    options.Conventions.AuthorizeFolder("/Statistics");
})
.AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapHealthChecks("/health");

app.MapGet("/api/badge-count", async (HttpContext httpContext, ApplicationDbContext context, UserManager<IdentityUser> userManager) =>
{
    var userId = userManager.GetUserId(httpContext.User);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var count = await BadgeCountProvider.GetCountAsync(context, userId, DateOnly.FromDateTime(DateTime.Now));
    return Results.Ok(new { count });
});

app.Run();
