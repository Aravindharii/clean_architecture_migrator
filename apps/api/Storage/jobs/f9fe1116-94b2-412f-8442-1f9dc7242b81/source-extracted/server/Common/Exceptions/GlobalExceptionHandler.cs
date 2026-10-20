using System.Net;
using LibraryManagement.Api.Common.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibraryManagement.Api.Common.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is BookCannotBeDeletedException bcd)
        {
            _logger.LogWarning(
                "Book {BookId} cannot be deleted: {Reason}",
                bcd.BookId,
                bcd.Reason);

            var errorResponse = new ApiResponse<object>
            {
                Success = false,
                Message = bcd.Message,
                Data = new
                {
                    CanDelete = false,
                    bcd.Reason,
                    bcd.TotalCopies,
                    bcd.AvailableCopies,
                    bcd.LoanedCopies
                }
            };

            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
            return true;
        }

        var (statusCode, message) = exception switch
        {
            NotFoundException nf => (HttpStatusCode.NotFound, nf.Message),
            ConflictException cf => (HttpStatusCode.Conflict, cf.Message),
            PreconditionFailedException pf => (HttpStatusCode.PreconditionFailed, pf.Message),
            BadRequestException br => (HttpStatusCode.BadRequest, br.Message),
            ForbiddenException fb => (HttpStatusCode.Forbidden, fb.Message),
            AppException app => (HttpStatusCode.BadRequest, app.Message),
            OperationCanceledException => (HttpStatusCode.BadRequest, "The request was cancelled."),
            DbUpdateException db => ClassifyDbUpdateException(db, out var dbMessage) switch
            {
                HttpStatusCode.Conflict => (HttpStatusCode.Conflict, dbMessage),
                HttpStatusCode.BadRequest => (HttpStatusCode.BadRequest, dbMessage),
                _ => (HttpStatusCode.InternalServerError, dbMessage)
            },
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception: {ExceptionType}: {Message}",
                exception.GetType().Name,
                exception.Message);
        }
        else if (exception is not AppException && exception is not DbUpdateException)
        {
            _logger.LogWarning(
                exception,
                "Request error: {ExceptionType}: {Message}",
                exception.GetType().Name,
                exception.Message);
        }

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        };

        httpContext.Response.StatusCode = (int)statusCode;

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private HttpStatusCode ClassifyDbUpdateException(DbUpdateException ex, out string message)
    {
        var postgresEx = ex.InnerException as PostgresException;
        var sqlState = postgresEx?.SqlState;
        var constraintName = postgresEx?.ConstraintName;
        var detail = postgresEx?.Detail;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["SqlState"] = sqlState ?? "N/A",
            ["ConstraintName"] = constraintName ?? "N/A",
            ["Detail"] = detail ?? "N/A",
        }))
        {
            _logger.LogError(
                ex,
                "Unhandled database exception. SQL state: {SqlState}, Constraint: {ConstraintName}, Detail: {Detail}",
                sqlState ?? "N/A",
                constraintName ?? "N/A",
                detail ?? "N/A");
        }

        message = sqlState switch
        {
            "23505" => "A record with the same unique value already exists.",
            "23503" => "The operation references a record that does not exist.",
            "23502" => "A required field was left empty.",
            _ => ex.InnerException?.Message ?? ex.Message
        };

        return sqlState switch
        {
            "23505" => HttpStatusCode.Conflict,
            "23503" or "23502" => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };
    }
}
