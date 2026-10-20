using Project.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Project.Infrastructure.Common;

public static class DatabaseExceptionHelper
{
    public static void HandleDbUpdateException(DbUpdateException ex, string operation, string entity, ILogger logger)
    {
        var postgresEx = ex.InnerException as PostgresException;
        var sqlState = postgresEx?.SqlState;
        var constraintName = postgresEx?.ConstraintName;
        var detail = postgresEx?.Detail;
        var innerMessage = ex.InnerException?.Message ?? ex.Message;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["SqlState"] = sqlState ?? "N/A",
            ["ConstraintName"] = constraintName ?? "N/A",
            ["Detail"] = detail ?? "N/A",
            ["Operation"] = operation,
            ["Entity"] = entity
        }))
        {
            logger.LogError(
                ex,
                "Database error while {Operation} {Entity}. SQL state: {SqlState}, Constraint: {ConstraintName}, Detail: {Detail}",
                operation,
                entity,
                sqlState ?? "N/A",
                constraintName ?? "N/A",
                detail ?? "N/A");
        }

        throw sqlState switch
        {
            "23505" => new ConflictException($"A {entity} with the same unique value already exists."),
            "23503" => new BadRequestException($"The {entity} references a record that does not exist."),
            "23502" => new BadRequestException($"A required field in the {entity} was left empty."),
            _ => new BadRequestException($"Database error while {operation} the {entity}: {innerMessage}")
        };
    }
}
