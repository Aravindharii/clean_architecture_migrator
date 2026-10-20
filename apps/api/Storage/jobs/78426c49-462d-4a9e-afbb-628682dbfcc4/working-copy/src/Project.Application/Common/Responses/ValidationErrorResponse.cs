namespace Project.Application.Common.Responses;

public class ValidationErrorResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Dictionary<string, string[]> Errors { get; set; } = new();
}
