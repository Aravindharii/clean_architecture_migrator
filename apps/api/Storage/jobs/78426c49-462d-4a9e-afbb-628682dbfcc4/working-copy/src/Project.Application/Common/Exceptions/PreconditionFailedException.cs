namespace Project.Application.Common.Exceptions;

public class PreconditionFailedException : AppException
{
    public PreconditionFailedException(string message)
        : base(message)
    {
    }
}
