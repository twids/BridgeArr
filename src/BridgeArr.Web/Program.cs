using BridgeArr.Api;
using BridgeArr.Application;
using BridgeArr.Infrastructure;
using BridgeArr.Infrastructure.Data;
using BridgeArr.Infrastructure.Seed;
using BridgeArr.Plugins.Plex;
using BridgeArr.Plugins.Radarr;
using BridgeArr.Plugins.Sonarr;
using Microsoft.AspNetCore.Identity;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using static BridgeArr.Infrastructure.Seed.DatabaseSeeder;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting BridgeArr Web host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/bridgearr-.log", rollingInterval: RollingInterval.Day));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddBridgeArrApi();
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddRadarrPlugin();
    builder.Services.AddSonarrPlugin();
    builder.Services.AddPlexPlugin();

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 4;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
    });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(AdminRole, policy => policy.RequireRole(AdminRole));

    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new() { Title = "BridgeArr API", Version = "v1" });
    });

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation());

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    app.MapRazorComponents<BridgeArr.Web.Components.App>()
        .AddInteractiveServerRenderMode();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await DatabaseSeeder.SeedAsync(app.Services);
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "BridgeArr Web host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

namespace BridgeArr.Web
{
    public partial class Program;
}
