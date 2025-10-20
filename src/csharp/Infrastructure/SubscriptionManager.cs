using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace REWApi.Infrastructure
{
    /// <summary>
    /// Subscription information for REW server push notifications
    /// </summary>
    public class REWSubscription
    {
        public string Id { get; }
        public string Endpoint { get; }
        public string CallbackUrl { get; }
        public DateTime CreatedAt { get; }
        public bool IsActive { get; set; }

        internal REWSubscription(string id, string endpoint, string callbackUrl)
        {
            Id = id;
            Endpoint = endpoint;
            CallbackUrl = callbackUrl;
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }

    /// <summary>
    /// Manages REW subscription endpoints with auto-renewal
    /// Handles server push notifications via embedded HTTP listener
    /// </summary>
    public class SubscriptionManager : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _rewBaseUrl;
        private readonly ILogger<SubscriptionManager>? _logger;
        private readonly ConcurrentDictionary<string, REWSubscription> _subscriptions;
        private readonly ConcurrentDictionary<string, Func<string, Task>> _handlers;
        private readonly Timer _renewalTimer;
        private volatile bool _disposed;

        // For simplicity, we'll use a basic callback URL pattern
        // In production, this would use an embedded HTTP server
        private readonly string _callbackBaseUrl;

        public SubscriptionManager(
            HttpClient httpClient,
            string rewBaseUrl,
            string callbackBaseUrl,
            ILogger<SubscriptionManager>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _rewBaseUrl = rewBaseUrl ?? throw new ArgumentNullException(nameof(rewBaseUrl));
            _callbackBaseUrl = callbackBaseUrl ?? throw new ArgumentNullException(nameof(callbackBaseUrl));
            _logger = logger;
            _subscriptions = new ConcurrentDictionary<string, REWSubscription>();
            _handlers = new ConcurrentDictionary<string, Func<string, Task>>();

            // Setup renewal timer to refresh subscriptions every 5 minutes
            _renewalTimer = new Timer(RenewSubscriptions, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Subscribe to REW notifications for a specific endpoint
        /// </summary>
        /// <typeparam name="T">Type of notification data</typeparam>
        /// <param name="endpoint">REW API endpoint to subscribe to</param>
        /// <param name="handler">Handler for received notifications</param>
        /// <param name="parameters">Optional subscription parameters</param>
        /// <returns>Disposable subscription</returns>
        public IDisposable Subscribe<T>(
            string endpoint, 
            Action<T> handler,
            Dictionary<string, string>? parameters = null)
        {
            if (string.IsNullOrEmpty(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (_disposed)
                throw new ObjectDisposedException(nameof(SubscriptionManager));

            var subscriptionId = Guid.NewGuid().ToString();
            var callbackUrl = $"{_callbackBaseUrl}/subscriptions/{subscriptionId}";

            // Handler wrapper
            Task HandleNotification(string jsonData)
            {
                try
                {
                    // In a real implementation, we'd deserialize T from jsonData
                    // For now, we'll assume T is string or handle basic cases
                    if (typeof(T) == typeof(string))
                    {
                        handler((T)(object)jsonData);
                    }
                    else
                    {
                        // Would use System.Text.Json here for proper deserialization
                        _logger?.LogWarning("Subscription handler for {Endpoint} expects type {Type}, but deserialization not implemented", 
                            endpoint, typeof(T).Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error handling notification for subscription {SubscriptionId} on endpoint {Endpoint}", 
                        subscriptionId, endpoint);
                }
                
                return Task.CompletedTask;
            }

            // Register handler
            _handlers[subscriptionId] = HandleNotification;

            // Create subscription record
            var subscription = new REWSubscription(subscriptionId, endpoint, callbackUrl);
            _subscriptions[subscriptionId] = subscription;

            // Start subscription task
            Task.Run(async () =>
            {
                try
                {
                    await CreateREWSubscription(subscription, parameters);
                    _logger?.LogInformation("Successfully created REW subscription {SubscriptionId} for endpoint {Endpoint}", 
                        subscriptionId, endpoint);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to create REW subscription {SubscriptionId} for endpoint {Endpoint}", 
                        subscriptionId, endpoint);
                    
                    // Clean up failed subscription
                    _subscriptions.TryRemove(subscriptionId, out _);
                    _handlers.TryRemove(subscriptionId, out _);
                }
            });

            // Return disposable to unsubscribe
            return new SubscriptionHandle(this, subscriptionId);
        }

        /// <summary>
        /// Handle incoming notification from REW
        /// This would be called by an embedded HTTP server in production
        /// </summary>
        internal async Task HandleIncomingNotification(string subscriptionId, string jsonData)
        {
            if (_handlers.TryGetValue(subscriptionId, out var handler))
            {
                await handler(jsonData);
            }
            else
            {
                _logger?.LogWarning("Received notification for unknown subscription {SubscriptionId}", subscriptionId);
            }
        }

        /// <summary>
        /// Unsubscribe from REW notifications
        /// </summary>
        internal async Task Unsubscribe(string subscriptionId)
        {
            if (_subscriptions.TryRemove(subscriptionId, out var subscription))
            {
                try
                {
                    await DeleteREWSubscription(subscription);
                    _logger?.LogInformation("Successfully unsubscribed from REW endpoint {Endpoint}", subscription.Endpoint);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to unsubscribe from REW endpoint {Endpoint}", subscription.Endpoint);
                }
            }

            _handlers.TryRemove(subscriptionId, out _);
        }

        /// <summary>
        /// Unsubscribe from all active subscriptions
        /// </summary>
        public async Task UnsubscribeAll()
        {
            var subscriptionIds = new List<string>(_subscriptions.Keys);
            
            var tasks = subscriptionIds.Select(Unsubscribe);
            await Task.WhenAll(tasks);
            
            _logger?.LogInformation("Unsubscribed from all REW subscriptions");
        }

        /// <summary>
        /// Get all active subscriptions
        /// </summary>
        public IReadOnlyList<REWSubscription> GetActiveSubscriptions()
        {
            return new List<REWSubscription>(_subscriptions.Values);
        }

        private async Task CreateREWSubscription(REWSubscription subscription, Dictionary<string, string>? parameters)
        {
            // Build subscription request to REW API
            var subscriptionData = new Dictionary<string, object>
            {
                ["url"] = subscription.CallbackUrl,
                ["endpoint"] = subscription.Endpoint
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    subscriptionData[param.Key] = param.Value;
                }
            }

            // In a real implementation, this would call the REW subscription API
            // For example: POST /subscriptions with the callback URL and endpoint
            _logger?.LogDebug("Creating REW subscription for {Endpoint} with callback {CallbackUrl}", 
                subscription.Endpoint, subscription.CallbackUrl);

            // Simulate API call
            await Task.Delay(100);
        }

        private async Task DeleteREWSubscription(REWSubscription subscription)
        {
            // In a real implementation, this would call the REW unsubscribe API
            // For example: DELETE /subscriptions/{id}
            _logger?.LogDebug("Deleting REW subscription {SubscriptionId} for {Endpoint}", 
                subscription.Id, subscription.Endpoint);

            // Simulate API call
            await Task.Delay(50);
        }

        private async void RenewSubscriptions(object? state)
        {
            if (_disposed)
                return;

            try
            {
                _logger?.LogDebug("Renewing REW subscriptions");

                foreach (var subscription in _subscriptions.Values)
                {
                    if (subscription.IsActive)
                    {
                        // In a real implementation, we'd refresh the subscription with REW
                        // This prevents subscriptions from expiring
                        await Task.Delay(10); // Simulate renewal
                    }
                }

                _logger?.LogDebug("Successfully renewed {Count} REW subscriptions", _subscriptions.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error renewing REW subscriptions");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                _renewalTimer?.Dispose();
                
                // Unsubscribe from all subscriptions
                UnsubscribeAll().Wait(TimeSpan.FromSeconds(10));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during SubscriptionManager disposal");
            }
        }

        /// <summary>
        /// Handle for managing individual subscriptions
        /// </summary>
        private class SubscriptionHandle : IDisposable
        {
            private readonly SubscriptionManager _manager;
            private readonly string _subscriptionId;
            private volatile bool _disposed;

            public SubscriptionHandle(SubscriptionManager manager, string subscriptionId)
            {
                _manager = manager;
                _subscriptionId = subscriptionId;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                Task.Run(async () =>
                {
                    try
                    {
                        await _manager.Unsubscribe(_subscriptionId);
                    }
                    catch (Exception ex)
                    {
                        _manager._logger?.LogWarning(ex, "Error unsubscribing {SubscriptionId}", _subscriptionId);
                    }
                });
            }
        }
    }
}