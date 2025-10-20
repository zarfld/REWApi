using System;

namespace REWApi.Infrastructure
{
    /// <summary>
    /// Base exception for all REW API related errors
    /// </summary>
    public abstract class REWException : Exception
    {
        protected REWException() { }
        protected REWException(string message) : base(message) { }
        protected REWException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when REW API connection fails
    /// </summary>
    public class REWConnectionException : REWException
    {
        public string? BaseUrl { get; }

        public REWConnectionException(string baseUrl) : base($"Failed to connect to REW API at {baseUrl}")
        {
            BaseUrl = baseUrl;
        }

        public REWConnectionException(string baseUrl, Exception innerException) 
            : base($"Failed to connect to REW API at {baseUrl}", innerException)
        {
            BaseUrl = baseUrl;
        }
    }

    /// <summary>
    /// Exception thrown when REW audio configuration fails
    /// </summary>
    public class REWAudioException : REWException
    {
        public string[] AvailableDevices { get; }
        public int[] SupportedSampleRates { get; }

        public REWAudioException(string message, string[] availableDevices, int[] supportedSampleRates) 
            : base(message)
        {
            AvailableDevices = availableDevices;
            SupportedSampleRates = supportedSampleRates;
        }
    }

    /// <summary>
    /// Exception thrown when a REW command is already in progress
    /// </summary>
    public class REWCommandInProgressException : REWException
    {
        public string CurrentCommand { get; }

        public REWCommandInProgressException(string currentCommand) 
            : base($"REW command already in progress: {currentCommand}")
        {
            CurrentCommand = currentCommand;
        }
    }

    /// <summary>
    /// Exception thrown when a long-running job fails
    /// </summary>
    public class REWJobException : REWException
    {
        public string JobId { get; }
        public string JobType { get; }

        public REWJobException(string jobId, string jobType, string message) 
            : base($"REW job {jobType} ({jobId}) failed: {message}")
        {
            JobId = jobId;
            JobType = jobType;
        }
    }
}