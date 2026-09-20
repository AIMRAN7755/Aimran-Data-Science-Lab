using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AimranDataScienceLab.ServiceTemplates.Base;

namespace AimranDataScienceLab.ServiceTemplates.Deployment
{
    /// <summary>
    /// Template for hosting AIMRAN services as Windows Services.
    /// Inherits from ServiceBase for Windows service integration.
    /// </summary>
    public class WindowsServiceHost : System.ServiceProcess.ServiceBase
    {
        private ServiceHost _serviceHost;
        private IHost _host;
        private Task _runTask;

        public WindowsServiceHost()
        {
            ServiceName = "AIMRANService";
            DisplayName = "AIMRAN Data Science Lab Service";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        /// <summary>
        /// Called when the Windows Service starts.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            try
            {
                EventLog.WriteEntry(ServiceName, "Service starting...");

                // Build the host
                _host = CreateHostBuilder(args).Build();

                // Get the service host
                _serviceHost = _host.Services.GetRequiredService<ServiceHost>();

                // Run the service in a background task
                _runTask = _serviceHost.RunAsync();

                EventLog.WriteEntry(ServiceName, "Service started successfully");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ServiceName, $"Error starting service: {ex.Message}\n{ex.StackTrace}", 
                    EventLogEntryType.Error);
                throw;
            }
        }

        /// <summary>
        /// Called when the Windows Service stops.
        /// </summary>
        protected override void OnStop()
        {
            try
            {
                EventLog.WriteEntry(ServiceName, "Service stopping...");

                _serviceHost?.Shutdown();

                // Wait for graceful shutdown (with timeout)
                var stopTask = Task.Run(async () =>
                {
                    try
                    {
                        await (_runTask ?? Task.CompletedTask).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected during shutdown
                    }
                });

                if (!stopTask.Wait(TimeSpan.FromSeconds(30)))
                {
                    EventLog.WriteEntry(ServiceName, "Service shutdown timeout - forcing stop", 
                        EventLogEntryType.Warning);
                }

                EventLog.WriteEntry(ServiceName, "Service stopped");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(ServiceName, $"Error stopping service: {ex.Message}\n{ex.StackTrace}", 
                    EventLogEntryType.Error);
                throw;
            }
            finally
            {
                _host?.Dispose();
            }
        }

        /// <summary>
        /// Override this method to configure the host for your specific service.
        /// </summary>
        protected virtual IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((context, services) =>
                {
                    // Add your services here
                    services.AddLogging(builder =>
                    {
                        builder.AddEventLog();
                    });
                });
        }

        /// <summary>
        /// Entry point for running the Windows Service.
        /// </summary>
        public static void Main(string[] args)
        {
            var servicesToRun = new ServiceBase[]
            {
                new WindowsServiceHost()
            };
            ServiceBase.Run(servicesToRun);
        }
    }

    /// <summary>
    /// Standalone Windows Service runner that uses the standard hosted service.
    /// Can be used to install/uninstall the service.
    /// </summary>
    public static class WindowsServiceInstaller
    {
        /// <summary>
        /// Installs the Windows Service.
        /// </summary>
        public static void Install(string serviceName, string displayName, string executablePath)
        {
            try
            {
                using (var process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"create {serviceName} binPath= \"{executablePath}\" displayName= \"{displayName}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode != 0)
                    {
                        throw new Exception($"Service installation failed with exit code {process?.ExitCode}");
                    }
                }

                System.Console.WriteLine($"Service '{serviceName}' installed successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error installing service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Uninstalls the Windows Service.
        /// </summary>
        public static void Uninstall(string serviceName)
        {
            try
            {
                // Stop the service first
                using (var process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "net.exe",
                        Arguments = $"stop {serviceName}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }))
                {
                    process?.WaitForExit();
                }

                // Delete the service
                using (var process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"delete {serviceName}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode != 0)
                    {
                        throw new Exception($"Service deletion failed with exit code {process?.ExitCode}");
                    }
                }

                System.Console.WriteLine($"Service '{serviceName}' uninstalled successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error uninstalling service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Starts a stopped Windows Service.
        /// </summary>
        public static void Start(string serviceName)
        {
            try
            {
                using (var process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "net.exe",
                        Arguments = $"start {serviceName}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode != 0)
                    {
                        throw new Exception($"Service start failed with exit code {process?.ExitCode}");
                    }
                }

                System.Console.WriteLine($"Service '{serviceName}' started");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error starting service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops a running Windows Service.
        /// </summary>
        public static void Stop(string serviceName)
        {
            try
            {
                using (var process = System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "net.exe",
                        Arguments = $"stop {serviceName}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode != 0)
                    {
                        throw new Exception($"Service stop failed with exit code {process?.ExitCode}");
                    }
                }

                System.Console.WriteLine($"Service '{serviceName}' stopped");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error stopping service: {ex.Message}");
                throw;
            }
        }
    }
}
