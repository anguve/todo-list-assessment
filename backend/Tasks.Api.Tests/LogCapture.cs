using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Tasks.Api.Tests;

public sealed class LogCapture : ILoggerProvider, ILogger
{
    private static readonly ConcurrentQueue<string> Lines = new();

    /// <summary>Drops the lines captured so far.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static void Clear()
    {
        while (Lines.TryDequeue(out _))
        {
        }
    }

    /// <summary>Joins the captured lines into one string.</summary>
    /// <returns>The log text.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static string Text() => string.Join('\n', Lines);

    /// <summary>Returns this capture as the logger for every category.</summary>
    /// <param name="categoryName">The logger category. It is ignored.</param>
    /// <returns>This instance.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public ILogger CreateLogger(string categoryName) => this;

    /// <summary>Nothing is held open, so disposal is a no-op.</summary>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public void Dispose()
    {
    }

    /// <summary>Scopes are not stored.</summary>
    /// <typeparam name="TState">The scope state type.</typeparam>
    /// <param name="state">The scope state. It is ignored.</param>
    /// <returns>Null.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>Every level is captured.</summary>
    /// <param name="logLevel">The level of the event.</param>
    /// <returns>True.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <summary>Stores the formatted message and, when present, the exception message.</summary>
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
        var line = formatter(state, exception);
        if (exception is not null)
        {
            line = $"{line} {exception.Message}";
        }

        Lines.Enqueue(line);
    }
}
