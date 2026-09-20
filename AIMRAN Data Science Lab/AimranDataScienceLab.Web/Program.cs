using AimranDataScienceLab.Web.Components;
using AIMRAN_Data_Science_Lab.Web.Hubs;
using AIMRAN_Data_Science_Lab.Services.Collaboration;
using AIMRAN_Data_Science_Lab.Services.Notifications;
using AIMRAN_Data_Science_Lab.Services.Settings;
using AimranDataScienceLab.Engine;
using MudBlazor.Services;
using Serilog;
using Serilog.Events;

// ════════════════════════════════════════════════════════════════════════════════
// AIMRAN Data Science Lab - Enterprise Application Startup
// ════════════════════════════════════════════════════════════════════════════════

try
{
    // ── Serilog Configuration for Enterprise Logging ────────────────────────────
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "AIMRAN-DataScienceLab")
        .Enrich.WithProperty("Version", "1.0.0")
        .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/.aimran-dsl-{Date}.log",
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30)
        .CreateLogger();

    Log.Information("═══════════════════════════════════════════════════════════");
    Log.Information("🚀 Starting AIMRAN Data Science Lab v1.0.0");
    Log.Information("═══════════════════════════════════════════════════════════");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── Blazor Interactive Server ────────────────────────────────────────────────
    Log.Information("📝 Configuring Blazor Interactive Server components...");
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // ── MudBlazor Services ───────────────────────────────────────────────────────
    Log.Information("🎨 Configuring MudBlazor UI framework...");
    builder.Services.AddMudServices(config =>
    {
        config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;
        config.SnackbarConfiguration.PreventDuplicates = true;
        config.SnackbarConfiguration.NewestOnTop = false;
        config.SnackbarConfiguration.ShowCloseIcon = true;
        config.SnackbarConfiguration.VisibleStateDuration = 10000;
    });

    // ── Settings Service ─────────────────────────────────────────────────────────
    Log.Information("⚙️ Registering Settings service...");
    builder.Services.AddScoped<ISettingsService, InMemorySettingsService>();

    // ── SignalR for Real-Time Collaboration ──────────────────────────────────────
    Log.Information("🔄 Configuring SignalR for real-time collaboration...");
    builder.Services.AddSignalR(options =>
    {
        options.MaximumReceiveMessageSize = 32 * 1024 * 1024; // 32 MB
        options.EnableDetailedErrors = !builder.Environment.IsProduction();
    });

    // ── Real-Time Collaboration Services ─────────────────────────────────────────
    Log.Information("👥 Registering collaboration services (presence, conflict resolution, notifications)...");
    builder.Services.AddScoped<IPresenceService, PresenceService>();
    builder.Services.AddScoped<ICollaborationService, CollaborationService>();
    builder.Services.AddScoped<IConflictResolutionService, ConflictResolutionService>();
    builder.Services.AddScoped<IRealTimeNotificationService, RealTimeNotificationService>();

    // ── Register the Full AIMRAN Data Science Lab Service Stack ──────────────────
    Log.Information("🔧 Initializing AIMRAN Data Science Lab service stack...");
    builder.Services.AddAimranDataScienceLab();

    Log.Information("✅ All services registered successfully");

    var app = builder.Build();

    // ── Configure Middleware Pipeline ────────────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        Log.Information("🔒 Production environment detected - enabling exception handler and HSTS...");
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    else
    {
        Log.Information("💻 Development environment detected - enabling detailed error pages...");
    }

    // ── HTTPS Redirection Configuration ──────────────────────────────────────────
    var configuredUrls = builder.Configuration["ASPNETCORE_URLS"];
    var hasHttpsBinding = configuredUrls?
        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) == true;

    if (!app.Environment.IsDevelopment() || hasHttpsBinding)
    {
        Log.Information("🔐 HTTPS redirection enabled");
        app.UseHttpsRedirection();
    }

    app.UseStaticFiles();
    app.UseAntiforgery();

    // ── SignalR Hub Mapping ──────────────────────────────────────────────────────
    Log.Information("🌐 Mapping SignalR hubs...");
    app.MapHub<CollaborationHub>("/collaborationhub");

    // Register the Shared RCL assembly so its @page components are discoverable
    // for server-side routing. All routable pages live in the Shared library
    // (root namespace AIMRAN_Data_Science_Lab), not in this Web host assembly.
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .AddAdditionalAssemblies(typeof(AIMRAN_Data_Science_Lab.Components.Layout.MainLayout).Assembly);

    Log.Information("═══════════════════════════════════════════════════════════");
    Log.Information("🎉 AIMRAN Data Science Lab is ready to serve requests");
    Log.Information("═══════════════════════════════════════════════════════════");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

