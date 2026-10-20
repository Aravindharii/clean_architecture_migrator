namespace Project.Application.Common.Exceptions;

/// <summary>
/// Represents an error when attempting to create
/// a resource that already exists.
/// </summary>
public class DuplicateResourceException : Exception
{
    public DuplicateResourceException(string message)
        : base(message)
    {
    }
}