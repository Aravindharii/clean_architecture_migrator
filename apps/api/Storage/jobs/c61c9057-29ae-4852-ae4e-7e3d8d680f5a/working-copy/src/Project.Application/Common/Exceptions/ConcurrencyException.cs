namespace Project.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a resource was modified
/// after the client last retrieved it.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ConcurrencyException"/> class.
    /// </summary>
    /// <param name="message">
    /// Message describing the concurrency conflict.
    /// </param>
    public ConcurrencyException(string message)
        : base(message)
    {
    }
}