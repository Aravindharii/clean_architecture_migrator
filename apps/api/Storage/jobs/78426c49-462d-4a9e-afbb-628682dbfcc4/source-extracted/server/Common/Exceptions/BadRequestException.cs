namespace LibraryManagement.Api.Common.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(message)
    {
    }
}
