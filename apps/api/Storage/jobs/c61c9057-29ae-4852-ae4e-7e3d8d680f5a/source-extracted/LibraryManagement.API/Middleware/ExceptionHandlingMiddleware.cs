using System.Net;
using System.Text.Json;
using FluentValidation;
using LibraryManagement.API.Exceptions;

namespace LibraryManagement.API.Middleware;

/// <summary>
/// Handles unhandled exceptions globally and
/// returns appropriate HTTP responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">
    /// The next middleware in the request pipeline.
    /// </param>
    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to handle the current HTTP request.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context.
    /// </param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    /// <summary>
    /// Handles exceptions and returns a consistent JSON response.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context.
    /// </param>
    /// <param name="exception">
    /// The exception that was thrown.
    /// </param>
    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => HttpStatusCode.NotFound,

            DuplicateResourceException => HttpStatusCode.Conflict,

            ConcurrencyException => HttpStatusCode.Conflict,

            ConflictException => HttpStatusCode.Conflict,

            ValidationException => HttpStatusCode.BadRequest,

            UnauthorizedAccessException => HttpStatusCode.Unauthorized,

            _ => HttpStatusCode.InternalServerError
        };


        object response;

        if (exception is ValidationException validationException)
        {
            response = new
            {
                StatusCode = (int)statusCode,
                Message = "Validation failed.",
                Errors = validationException.Errors.Select(error => new
                {
                    Field = error.PropertyName,
                    Message = error.ErrorMessage
                })
            };
        }
        else
        {
            response = new
            {
                StatusCode = (int)statusCode,
                Message = exception.Message
            };
        }


        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}