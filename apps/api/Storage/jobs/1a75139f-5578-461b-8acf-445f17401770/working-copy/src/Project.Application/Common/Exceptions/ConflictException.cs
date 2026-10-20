namespace Project.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an operation conflicts with
/// the current state of a resource.
/// </summary>
public class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ConflictException"/> class.
    /// </summary>
    /// <param name="message">
    /// Message describing the conflict.
    /// </param>
    public ConflictException(string message)
        : base(message)
    {
    }
}