using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AimranDataScienceLab.ServiceTemplates.Base.Infrastructure
{
    /// <summary>
    /// Base class for the IPC Broker service in the AIMRAN ecosystem.
    /// The IPC Broker handles inter-process communication, message routing, and event distribution.
    /// </summary>
    public abstract class IPCBrokerServiceBase : ServiceBase
    {
        protected IPCBrokerServiceBase(ILogger<ServiceBase> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Gets the IPC broker capabilities.
        /// </summary>
        protected override string[] GetServiceCapabilities() =>
            new[]
            {
                "Message Routing",
                "Event Distribution",
                "Publish/Subscribe",
                "Message Persistence",
                "Dead Letter Handling",
                "Connection Management",
                "Stream Processing"
            };

        /// <summary>
        /// Gets the standard dependencies for IPC broker services.
        /// </summary>
        protected override string[] GetServiceDependencies() =>
            new[]
            {
                "ServiceRegistry",
                "PersistentStorage",
                "MonitoringService",
                "EventBus"
            };

        /// <summary>
        /// Gets IPC broker-specific service tags.
        /// </summary>
        protected override Dictionary<string, string> GetServiceTags() =>
            new()
            {
                { "role", "infrastructure" },
                { "tier", "messaging" },
                { "criticality", "high" },
                { "stateful", "true" }
            };

        /// <summary>
        /// Gets IPC broker-specific configuration.
        /// </summary>
        protected override ServiceConfiguration GetServiceConfiguration() =>
            new()
            {
                MaxConcurrentRequests = 50000,
                RequestTimeout = TimeSpan.FromSeconds(5),
                HealthCheckInterval = TimeSpan.FromSeconds(10),
                Features = new()
                {
                    { "PersistentQueue", "true" },
                    { "PublishSubscribe", "true" },
                    { "MessageRoutine", "true" },
                    { "DeadLetterQueue", "true" }
                }
            };

        /// <summary>
        /// Gets the service description.
        /// </summary>
        protected override string GetServiceDescription() =>
            "AIMRAN IPC Broker - handles inter-process communication, message routing, and event distribution";

        /// <summary>
        /// Override this method to implement message publishing.
        /// </summary>
        protected virtual Task PublishMessageAsync(
            string topicName,
            Dictionary<string, object?> message,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("PublishMessageAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to implement topic subscription.
        /// </summary>
        protected virtual Task<string> SubscribeToTopicAsync(
            string topicName,
            string subscriberId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("SubscribeToTopicAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to consume messages from a subscription.
        /// </summary>
        protected virtual Task<List<Dictionary<string, object?>>> ConsumeMessagesAsync(
            string subscriberId,
            int maxMessages = 100,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("ConsumeMessagesAsync must be implemented in derived class");
        }

        /// <summary>
        /// Override this method to unsubscribe from a topic.
        /// </summary>
        protected virtual Task UnsubscribeFromTopicAsync(
            string subscriberId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("UnsubscribeFromTopicAsync must be implemented in derived class");
        }
    }
}
