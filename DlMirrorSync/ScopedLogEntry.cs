namespace DlMirrorSync;

/// <summary>
/// Represents a scope for logging information messages.
/// </summary>
public sealed class ScopedLogEntry : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _message;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedLogEntry"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="message">The message to log.</param>
    public ScopedLogEntry(ILogger logger, string message)
    {
        _logger = logger;
        _message = message;
        _logger.LogInformation("Entering {message}", _message);
    }

    /// <summary>
    /// Log the message indicating end of scope
    /// </summary>
    public void Dispose()
    {
        _logger.LogInformation("Exiting {message}", _message);
    }
}
