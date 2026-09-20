using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base
{
    /// <summary>
    /// Generic host wrapper that bridges Windows Service lifecycle events
    /// (OnStart / OnStop from <see cref="System.ServiceProcess.ServiceBase"/>)
    /// to the .NET Generic Host cancellation model.
    ///
    /// Usage:
    /// <code>
    ///   var host = Host.CreateDefaultBuilder(args).Build();
    ///   var serviceHost = host.Services.GetRequiredService&lt;ServiceHost&gt;();
    ///   // Called by WindowsServiceHost.OnStart:
    ///   _ = serviceHost.RunAsync();
    ///   // Called by WindowsServiceHost.OnStop:
    ///   serviceHost.Shutdown();
    /// </code>
    /// </summary>
    public sealed class ServiceHost
    {
        private readonly IHost _host;
        private readonly ILogger<ServiceHost> _logger;
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// Initialises the service host with an already-built <see cref="IHost"/>.
        /// Register this class in DI via <c>services.AddSingleton&lt;ServiceHost&gt;()</c>.
        /// </summary>
        public ServiceHost(IHost host, ILogger<ServiceHost> logger)
        {
            _host   = host   ?? throw new ArgumentNullException(nameof(host));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts all hosted services and runs until <see cref="Shutdown"/> is called
        /// or the process receives a termination signal.
        /// </summary>
        public async Task RunAsync()
        {
            _logger.LogInformation("ServiceHost: starting hosted services");
            try
            {
                await _host.RunAsync(_cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown path — swallow.
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "ServiceHost: unhandled exception in RunAsync");
                throw;
            }
            finally
            {
                _logger.LogInformation("ServiceHost: all hosted services stopped");
            }
        }

        /// <summary>
        /// Requests a graceful shutdown of all hosted services.
        /// Safe to call from any thread.
        /// </summary>
        public void Shutdown()
        {
            if (_cts.IsCancellationRequested)
                return;

            _logger.LogInformation("ServiceHost: shutdown requested");
            _cts.Cancel();
        }

        /// <summary>
        /// Frees the cancellation token source.
        /// </summary>
        public void Dispose() => _cts.Dispose();
    }
}
