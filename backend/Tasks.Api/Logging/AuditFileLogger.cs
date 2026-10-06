namespace Tasks.Api.Logging;

public sealed class AuditFileLogger(string path) : ILoggerProvider
{
    private readonly Lock gate = new();

    /// <summary>
    /// Creates a logger that appends audit and error lines to the file.
    /// </summary>
    /// <param name="categoryName">The logger category.</param>
    /// <returns>A file logger for that category.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, path, gate);

    /// <summary>
    /// Nothing is held open between writes, so disposal is a no-op.
    /// </summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public void Dispose()
    {
    }

    private sealed class FileLogger(string category, string path, Lock gate) : ILogger
    {
        /// <summary>
        /// Scopes are not written to the file.
        /// </summary>
        /// <typeparam name="TState">The scope state type.</typeparam>
        /// <param name="state">The scope state. It is ignored.</param>
        /// <returns>Null.</returns>
        /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        /// <summary>
        /// Accepts the audit category and the two error loggers.
        /// </summary>
        /// <param name="logLevel">The level of the event.</param>
        /// <returns>True when this category is written to the file.</returns>
        /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
        public bool IsEnabled(LogLevel logLevel) =>
            category is ActivityLog.Category
                or "Tasks.Api.Logging.ErrorCaptureMiddleware"
                or "Tasks.Api.Logging.ErrorLoggingHandler";

        /// <summary>
        /// Appends one audit or error line. Passwords and tokens are not added here.
        /// </summary>
        /// <typeparam name="TState">The logger state type.</typeparam>
        /// <param name="logLevel">The level of the event.</param>
        /// <param name="eventId">The event id.</param>
        /// <param name="state">The structured state.</param>
        /// <param name="exception">The exception, when there is one.</param>
        /// <param name="formatter">Builds the message text.</param>
        /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var line = $"{DateTimeOffset.UtcNow:O} {logLevel} {category} {formatter(state, exception)}";
            if (exception is not null)
            {
                line += $" {exception.GetType().Name}: {exception.Message}";
            }

            lock (gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
    }
}
