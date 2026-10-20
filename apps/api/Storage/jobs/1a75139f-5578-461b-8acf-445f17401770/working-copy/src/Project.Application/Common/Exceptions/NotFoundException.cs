namespace Project.Application.Common.Exceptions;

/// <summary>
/// Represents an error when the requested
/// resource cannot be found.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}