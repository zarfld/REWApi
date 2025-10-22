using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using REWApi.Infrastructure;

namespace REWApi.Domain.Services
{
    /// <summary>
    /// REW Groups management for organizing measurements
    /// Maps to /groups/* endpoints
    /// Follows IEEE 12207 Software Service Architecture patterns
    /// </summary>
    public class GroupsService
    {
        private readonly CommandGate _commandGate;
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<GroupsService>? _logger;
        
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        internal GroupsService(
            CommandGate commandGate,
            HttpClient httpClient,
            string baseUrl,
            ILogger<GroupsService>? logger = null)
        {
            _commandGate = commandGate ?? throw new ArgumentNullException(nameof(commandGate));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _logger = logger;
        }

        #region Group CRUD Operations

        /// <summary>
        /// Get list of all groups
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of all groups</returns>
        public async Task<List<GroupInfo>> ListGroups(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/groups";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<GroupInfo>>(content, JsonOptions) ?? new List<GroupInfo>();
        }

        /// <summary>
        /// Get specific group by UUID
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Group information</returns>
        public async Task<GroupInfo> GetGroup(string groupId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));

            var url = $"{_baseUrl}/groups/{groupId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<GroupInfo>(content, JsonOptions) ?? new GroupInfo();
        }

        /// <summary>
        /// Create a new group
        /// </summary>
        /// <param name="group">Group information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if group was created successfully</returns>
        public async Task<bool> CreateGroup(GroupInfo group, CancellationToken cancellationToken = default)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/groups";
                var json = JsonSerializer.Serialize(group);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                return response.IsSuccessStatusCode;
            }, "Create Group", cancellationToken);
        }

        /// <summary>
        /// Update an existing group (name and/or notes)
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="group">Updated group information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if group was updated successfully</returns>
        public async Task<bool> UpdateGroup(string groupId, GroupInfo group, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/groups/{groupId}";
                var json = JsonSerializer.Serialize(group);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                return response.IsSuccessStatusCode;
            }, "Update Group", cancellationToken);
        }

        /// <summary>
        /// Delete a specific group
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if group was deleted successfully</returns>
        public async Task<bool> DeleteGroup(string groupId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/groups/{groupId}";
                var response = await _httpClient.DeleteAsync(url, cancellationToken);
                return response.IsSuccessStatusCode;
            }, "Delete Group", cancellationToken);
        }

        /// <summary>
        /// Delete all groups
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all groups were deleted successfully</returns>
        public async Task<bool> DeleteAllGroups(CancellationToken cancellationToken = default)
        {
            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/groups";
                var response = await _httpClient.DeleteAsync(url, cancellationToken);
                return response.IsSuccessStatusCode;
            }, "Delete All Groups", cancellationToken);
        }

        #endregion

        #region Group Measurements Management

        /// <summary>
        /// Get all measurements in a group
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of measurements in the group</returns>
        public async Task<List<MeasurementSummary>> GetGroupMeasurements(string groupId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));

            var url = $"{_baseUrl}/groups/{groupId}/measurements";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MeasurementSummary>>(content, JsonOptions) ?? new List<MeasurementSummary>();
        }

        /// <summary>
        /// Add a measurement to a group
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="measurement">Measurement to add (UUID is used)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if measurement was added successfully</returns>
        public async Task<bool> AddMeasurementToGroup(string groupId, MeasurementSummary measurement, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (measurement == null)
                throw new ArgumentNullException(nameof(measurement));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/groups/{groupId}/measurements";
                var json = JsonSerializer.Serialize(measurement);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                return response.IsSuccessStatusCode;
            }, "Add Measurement to Group", cancellationToken);
        }

        /// <summary>
        /// Add a measurement to a group by UUID
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="measurementUuid">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if measurement was added successfully</returns>
        public async Task<bool> AddMeasurementToGroup(string groupId, string measurementUuid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(measurementUuid))
                throw new ArgumentException("Measurement UUID cannot be null or empty", nameof(measurementUuid));

            var measurement = new MeasurementSummary { UUID = measurementUuid };
            return await AddMeasurementToGroup(groupId, measurement, cancellationToken);
        }

        /// <summary>
        /// Remove a measurement from a group
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="measurementUuid">Measurement UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if measurement was removed successfully</returns>
        public async Task<bool> RemoveMeasurementFromGroup(string groupId, string measurementUuid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));
            if (string.IsNullOrWhiteSpace(measurementUuid))
                throw new ArgumentException("Measurement UUID cannot be null or empty", nameof(measurementUuid));

            return await _commandGate.ExecuteCommand(async () =>
            {
                var url = $"{_baseUrl}/groups/{groupId}/measurements/{measurementUuid}";
                var response = await _httpClient.DeleteAsync(url, cancellationToken);
                return response.IsSuccessStatusCode;
            }, "Remove Measurement from Group", cancellationToken);
        }

        #endregion

        #region Subscription Management

        /// <summary>
        /// Subscribe to group change notifications
        /// </summary>
        /// <param name="callbackUrl">URL to receive notifications</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if subscription was successful</returns>
        public async Task<bool> SubscribeToGroupChanges(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var url = $"{_baseUrl}/groups/subscribe";
            var subscriber = new { callbackUrl };
            var json = JsonSerializer.Serialize(subscriber);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Unsubscribe from group change notifications
        /// </summary>
        /// <param name="callbackUrl">URL that was subscribed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if unsubscription was successful</returns>
        public async Task<bool> UnsubscribeFromGroupChanges(string callbackUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                throw new ArgumentException("Callback URL cannot be null or empty", nameof(callbackUrl));

            var url = $"{_baseUrl}/groups/unsubscribe";
            var subscriber = new { callbackUrl };
            var json = JsonSerializer.Serialize(subscriber);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Get list of all subscribers to group changes
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of subscriber callback URLs</returns>
        public async Task<List<string>> GetSubscribers(CancellationToken cancellationToken = default)
        {
            var url = $"{_baseUrl}/groups/subscribers";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // API returns array of subscriber objects with callbackUrl property
            var subscribers = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(content, JsonOptions) ?? new List<Dictionary<string, string>>();
            return subscribers
                .Where(s => s.ContainsKey("callbackUrl"))
                .Select(s => s["callbackUrl"])
                .ToList();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if a group exists
        /// </summary>
        /// <param name="groupId">Group UUID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if group exists</returns>
        public async Task<bool> GroupExists(string groupId, CancellationToken cancellationToken = default)
        {
            try
            {
                await GetGroup(groupId, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Find groups by name (case-insensitive partial match)
        /// </summary>
        /// <param name="namePattern">Name pattern to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching groups</returns>
        public async Task<List<GroupInfo>> FindGroupsByName(string namePattern, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(namePattern))
                return new List<GroupInfo>();

            var allGroups = await ListGroups(cancellationToken);
            return allGroups
                .Where(g => g.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        #endregion
    }
}
