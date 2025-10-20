using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace REWApi.Infrastructure
{
    /// <summary>
    /// Progress information for long-running REW operations
    /// </summary>
    public class JobProgress
    {
        public int PercentComplete { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CurrentStep { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan? EstimatedRemaining { get; set; }
        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Handles REW's 200 (immediate) vs 202 (long-running) response pattern
    /// Provides unified async interface with progress reporting and cancellation
    /// </summary>
    /// <typeparam name="T">Type of the result when operation completes</typeparam>
    public class LongRunningJob<T>
    {
        private readonly Func<CancellationToken, Task<T>> _pollForCompletion;
        private readonly Func<Task> _cancelJob;
        private readonly ILogger? _logger;
        private readonly DateTime _startTime;

        public string JobId { get; }
        public string JobType { get; }
        public bool IsCompleted { get; private set; }
        public bool IsBlocking { get; }
        public T? Result { get; private set; }
        public Exception? Exception { get; private set; }

        internal LongRunningJob(
            string jobId,
            string jobType,
            bool isBlocking,
            Func<CancellationToken, Task<T>> pollForCompletion,
            Func<Task> cancelJob,
            ILogger? logger = null)
        {
            JobId = jobId;
            JobType = jobType;
            IsBlocking = isBlocking;
            _pollForCompletion = pollForCompletion;
            _cancelJob = cancelJob;
            _logger = logger;
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Create a job that is already completed (for 200 responses)
        /// </summary>
        internal static LongRunningJob<T> CompletedJob(T result, string jobType)
        {
            return new LongRunningJob<T>(
                jobId: Guid.NewGuid().ToString(),
                jobType: jobType,
                isBlocking: false,
                pollForCompletion: _ => Task.FromResult(result),
                cancelJob: () => Task.CompletedTask)
            {
                IsCompleted = true,
                Result = result
            };
        }

        /// <summary>
        /// Wait for the job to complete and return the result
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job result</returns>
        /// <exception cref="REWJobException">Thrown if job fails</exception>
        /// <exception cref="OperationCanceledException">Thrown if operation is cancelled</exception>
        public async Task<T> AwaitCompletion(CancellationToken cancellationToken = default)
        {
            if (IsCompleted)
            {
                if (Exception != null)
                    throw Exception;
                return Result!;
            }

            try
            {
                _logger?.LogInformation("Waiting for REW job {JobId} ({JobType}) to complete", JobId, JobType);
                
                var result = await _pollForCompletion(cancellationToken);
                
                IsCompleted = true;
                Result = result;
                
                var elapsed = DateTime.UtcNow - _startTime;
                _logger?.LogInformation("REW job {JobId} completed successfully in {Elapsed:mm\\:ss}", JobId, elapsed);
                
                return result;
            }
            catch (Exception ex)
            {
                IsCompleted = true;
                Exception = ex;
                
                var elapsed = DateTime.UtcNow - _startTime;
                _logger?.LogError(ex, "REW job {JobId} failed after {Elapsed:mm\\:ss}", JobId, elapsed);
                
                if (ex is OperationCanceledException)
                    throw;
                
                throw new REWJobException(JobId, JobType, ex.Message);
            }
        }

        /// <summary>
        /// Wait for the job to complete with progress reporting
        /// </summary>
        /// <param name="progress">Progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job result</returns>
        public async Task<T> AwaitCompletion(IProgress<JobProgress> progress, CancellationToken cancellationToken = default)
        {
            if (IsCompleted)
            {
                progress.Report(new JobProgress 
                { 
                    PercentComplete = 100, 
                    Status = "Completed",
                    Elapsed = TimeSpan.Zero
                });
                
                if (Exception != null)
                    throw Exception;
                return Result!;
            }

            // Report initial progress
            progress.Report(new JobProgress 
            { 
                PercentComplete = 0, 
                Status = "Starting",
                CurrentStep = JobType,
                Elapsed = TimeSpan.Zero
            });

            // For long-running jobs, we need to poll for progress
            // This would be implemented with actual REW API progress endpoints
            using var progressTimer = new Timer(
                callback: _ => ReportProgress(progress),
                state: null,
                dueTime: TimeSpan.FromSeconds(1),
                period: TimeSpan.FromSeconds(2));

            try
            {
                var result = await AwaitCompletion(cancellationToken);
                
                progress.Report(new JobProgress 
                { 
                    PercentComplete = 100, 
                    Status = "Completed",
                    Elapsed = DateTime.UtcNow - _startTime
                });
                
                return result;
            }
            catch
            {
                progress.Report(new JobProgress 
                { 
                    PercentComplete = 0, 
                    Status = "Failed",
                    Elapsed = DateTime.UtcNow - _startTime
                });
                throw;
            }
        }

        /// <summary>
        /// Cancel the running job
        /// </summary>
        public async Task Cancel()
        {
            if (IsCompleted)
                return;

            try
            {
                _logger?.LogInformation("Cancelling REW job {JobId} ({JobType})", JobId, JobType);
                await _cancelJob();
                IsCompleted = true;
                Exception = new OperationCanceledException($"REW job {JobId} was cancelled");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to cancel REW job {JobId}", JobId);
                // Don't throw - cancellation is best effort
            }
        }

        private void ReportProgress(IProgress<JobProgress> progress)
        {
            if (IsCompleted)
                return;

            var elapsed = DateTime.UtcNow - _startTime;
            
            // Estimate progress based on elapsed time (rough heuristic)
            var estimatedPercent = Math.Min(90, (int)(elapsed.TotalSeconds * 2));
            
            progress.Report(new JobProgress
            {
                PercentComplete = estimatedPercent,
                Status = "Processing",
                CurrentStep = JobType,
                Elapsed = elapsed,
                EstimatedRemaining = estimatedPercent > 0 
                    ? TimeSpan.FromSeconds(elapsed.TotalSeconds * (100 - estimatedPercent) / estimatedPercent)
                    : null
            });
        }
    }

    /// <summary>
    /// Factory for creating LongRunningJob instances
    /// </summary>
    internal static class LongRunningJob
    {
        /// <summary>
        /// Create a long-running job that polls for completion
        /// </summary>
        public static LongRunningJob<T> Create<T>(
            string jobId,
            string jobType,
            Func<CancellationToken, Task<T>> pollForCompletion,
            Func<Task> cancelJob,
            ILogger? logger = null)
        {
            return new LongRunningJob<T>(jobId, jobType, true, pollForCompletion, cancelJob, logger);
        }

        /// <summary>
        /// Create a job that is already completed
        /// </summary>
        public static LongRunningJob<T> Completed<T>(T result, string jobType)
        {
            return LongRunningJob<T>.CompletedJob(result, jobType);
        }
    }
}