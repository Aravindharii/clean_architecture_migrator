namespace LibraryManagement.Api.Common.Responses;

public interface IApiResponse
{
    object? Data { get; }
}

public class ApiResponse<T> : IApiResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    object? IApiResponse.Data => Data;
}