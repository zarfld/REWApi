using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace REWApi.Infrastructure
{
    /// <summary>
    /// Handles REW's single-command-in-progress constraint
    /// Ensures only one POST operation can execute at a time and provides emergency clear
    /// </summary>
    public class CommandGate : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private readonly Func<Task> _clearInProgressCommand;
        private readonly ILogger<CommandGate>? _logger;
        private volatile bool _disposed;
        private string? _currentCommand;
        private DateTime? _commandStartTime;

        public CommandGate(Func<Task> clearInProgressCommand, ILogger<CommandGate>? logger = null)
        {
            _gate = new SemaphoreSlim(1, 1);
            _clearInProgressCommand = clearInProgressCommand ?? throw new ArgumentNullException(nameof(clearInProgressCommand));
            _logger = logger;
        }

        /// <summary>
        /// Execute a REW command with single-flight guarantee
        /// </summary>
        /// <typeparam name="T">Return type of the command</typeparam>
        /// <param name="command">Command to execute</param>
        /// <param name="commandName">Name of the command for logging/diagnostics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Command result</returns>
        /// <exception cref="ObjectDisposedException">Thrown if CommandGate is disposed</exception>
        /// <exception cref="OperationCanceledException">Thrown if operation is cancelled</exception>
        public async Task<T> ExecuteCommand<T>(
            Func<Task<T>> command, 
            string commandName,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommandGate));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (string.IsNullOrEmpty(commandName))
                throw new ArgumentException("Command name cannot be null or empty", nameof(commandName));

            _logger?.LogDebug("Acquiring command gate for: {CommandName}", commandName);

            await _gate.WaitAsync(cancellationToken);

            try
            {
                _currentCommand = commandName;
                _commandStartTime = DateTime.UtcNow;

                _logger?.LogInformation("Executing REW command: {CommandName}", commandName);

                var result = await command();

                var elapsed = DateTime.UtcNow - _commandStartTime.Value;
                _logger?.LogInformation("REW command {CommandName} completed in {Elapsed:mm\\:ss\\.fff}", 
                    commandName, elapsed);

                return result;
            }
            catch (Exception ex)
            {
                var elapsed = _commandStartTime.HasValue 
                    ? DateTime.UtcNow - _commandStartTime.Value 
                    : TimeSpan.Zero;

                _logger?.LogError(ex, "REW command {CommandName} failed after {Elapsed:mm\\:ss\\.fff}", 
                    commandName, elapsed);

                // If command failed, try to clear any in-progress state
                try
                {
                    await ClearInProgress();
                }
                catch (Exception clearEx)
                {
                    _logger?.LogWarning(clearEx, "Failed to clear in-progress command after failure");
                }

                throw;
            }
            finally
            {
                _currentCommand = null;
                _commandStartTime = null;
                _gate.Release();
            }
        }

        /// <summary>
        /// Execute a REW command without return value
        /// </summary>
        /// <param name="command">Command to execute</param>
        /// <param name="commandName">Name of the command for logging/diagnostics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async Task ExecuteCommand(
            Func<Task> command, 
            string commandName,
            CancellationToken cancellationToken = default)
        {
            await ExecuteCommand(async () =>
            {
                await command();
                return true; // Dummy return value
            }, commandName, cancellationToken);
        }

        /// <summary>
        /// Clear any command currently in progress (emergency stop)
        /// This calls REW API's /application/clear-command-in-progress endpoint
        /// </summary>
        public async Task ClearInProgress()
        {
            if (_disposed)
                return;

            try
            {
                _logger?.LogWarning("Clearing REW command in progress");
                await _clearInProgressCommand();
                _logger?.LogInformation("Successfully cleared REW command in progress");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to clear REW command in progress");
                throw new REWConnectionException("Failed to clear command in progress", ex);
            }
        }

        /// <summary>
        /// Get information about the currently executing command
        /// </summary>
        public CommandStatus GetCurrentStatus()
        {
            return new CommandStatus
            {
                IsCommandInProgress = _currentCommand != null,
                CurrentCommand = _currentCommand,
                StartTime = _commandStartTime,
                Elapsed = _commandStartTime.HasValue ? DateTime.UtcNow - _commandStartTime.Value : null
            };
        }

        /// <summary>
        /// Check if the gate is currently locked (command in progress)
        /// </summary>
        public bool IsLocked => _gate.CurrentCount == 0;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // Try to clear any in-progress command before disposing
                if (_currentCommand != null)
                {
                    _clearInProgressCommand().Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to clear in-progress command during disposal");
            }

            _gate.Dispose();
        }
    }

    /// <summary>
    /// Status information for the command gate
    /// </summary>
    public class CommandStatus
    {
        public bool IsCommandInProgress { get; set; }
        public string? CurrentCommand { get; set; }
        public DateTime? StartTime { get; set; }
        public TimeSpan? Elapsed { get; set; }
    }
}