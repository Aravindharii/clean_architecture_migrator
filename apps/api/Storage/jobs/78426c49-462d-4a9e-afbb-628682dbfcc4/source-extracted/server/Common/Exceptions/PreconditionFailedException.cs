namespace LibraryManagement.Api.Common.Exceptions;

public class PreconditionFailedException : AppException
{
    public PreconditionFailedException(string message)
        : base(message)
    {
    }
}
