using System;

namespace SW.Logger.ElasticSerach
{
    /// <summary>
    /// Exception thrown when the SW ElasticSearch Logger is misconfigured (e.g. an invalid
    /// application name or a missing required setting is detected during startup).
    /// </summary>
    public class SWLoggerConfigurationException : Exception
    {
        /// <summary>Initialises a new instance with no message.</summary>
        public SWLoggerConfigurationException() { }

        /// <summary>Initialises a new instance with the specified error <paramref name="message"/>.</summary>
        /// <param name="message">Human-readable description of the configuration error.</param>
        public SWLoggerConfigurationException(string message) : base(message) { }

        /// <summary>
        /// Initialises a new instance with the specified error <paramref name="message"/> and a
        /// reference to the <paramref name="innerException"/> that is the cause of this exception.
        /// </summary>
        /// <param name="message">Human-readable description of the configuration error.</param>
        /// <param name="innerException">The exception that caused this exception.</param>
        public SWLoggerConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }
}