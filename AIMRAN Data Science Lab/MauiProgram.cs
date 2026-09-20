using AimranDataScienceLab.Engine;
using Microsoft.Extensions.Logging;

namespace AIMRAN_Data_Science_Lab
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // ── Register the full AIMRAN Data Science Lab service stack ───
            // Engine, SQLite persistence, data cleaning, dataset versioning,
            // Azure cloud services, Python/Rust gateway, plugin manager.
            builder.Services.AddAimranDataScienceLab();

            // ── Register Engine Manager for MAUI Desktop Deployment ───
            // Manages Python (8100), Rust (8200), R (8300) engine processes
            // Auto-start/stop, health checks, log streaming, process lifecycle
            builder.Services.AddScoped<IEngineManager>(provider =>
            {
                var httpClient = new HttpClient();
                return new EngineManager(httpClient);
            });

            // ── Configure Local Database Path for Windows Deployment ───
            // SQLite database stored in AppData folder for persistence
            ConfigureLocalDatabasePath();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // ── Initialize app lifecycle (engines, database, cleanup) ───
            // This runs after MauiApp is built
            var app = builder.Build();

            // Initialize engines on app startup
            _ = InitializeAppLifecycleAsync(app);

            return app;
        }

        /// <summary>
        /// Initialize app lifecycle after MauiApp is built.
        /// Initializes engine resources, auto-starts configured engines, sets up cleanup.
        /// </summary>
        private static async Task InitializeAppLifecycleAsync(MauiApp app)
        {
            try
            {
                var engineManager = app.Services.GetRequiredService<IEngineManager>();
                await AppLifecycleManager.InitializeAsync(engineManager);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize app lifecycle: {ex.Message}");
            }
        }

        /// <summary>
        /// Configure SQLite database to use AppData folder for local deployment on Windows.
        /// Creates folder structure if needed.
        /// </summary>
        private static void ConfigureLocalDatabasePath()
        {
            try
            {
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AIMRAN Lab"
                );

                // Ensure directory exists
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                // Create subdirectories
                var dbDir = Path.Combine(appDataPath, "Database");
                var enginesDir = Path.Combine(appDataPath, "Engines");
                var logsDir = Path.Combine(appDataPath, "Logs");
                var backupDir = Path.Combine(appDataPath, "Backups");

                foreach (var dir in new[] { dbDir, enginesDir, logsDir, backupDir })
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }

                // Store paths for use in services
                Environment.SetEnvironmentVariable("AIMRAN_APP_DATA", appDataPath);
                Environment.SetEnvironmentVariable("AIMRAN_DB_PATH", dbDir);
                Environment.SetEnvironmentVariable("AIMRAN_ENGINES_PATH", enginesDir);
                Environment.SetEnvironmentVariable("AIMRAN_LOGS_PATH", logsDir);
                Environment.SetEnvironmentVariable("AIMRAN_BACKUP_PATH", backupDir);

                System.Diagnostics.Debug.WriteLine($"✅ App data path initialized: {appDataPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to configure local database path: {ex}");
            }
        }
    }
}
