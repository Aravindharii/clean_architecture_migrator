using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LibraryManagement.API.Filters;

/// <summary>
/// Executes FluentValidation before controller actions.
/// If validation fails, returns 400 Bad Request.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Loop through all action parameters
        foreach (var argument in context.ActionArguments.Values)
        {
            // Skip null parameters
            if (argument is null)
                continue;

            // Build IValidator<T> based on the DTO type
            var validatorType = typeof(IValidator<>)
                .MakeGenericType(argument.GetType());

            // Resolve the validator from Dependency Injection
            var validator = _serviceProvider.GetService(validatorType);

            // No validator registered for this parameter
            if (validator is null)
                continue;

            // Create validation context
            var validationContext = new ValidationContext<object>(argument);

            // Execute FluentValidation
            var validationResult =
                await ((IValidator)validator)
                    .ValidateAsync(validationContext);

            // Validation failed
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(error => new
                    {
                        Field = error.PropertyName,
                        Message = error.ErrorMessage
                    });

                context.Result = new BadRequestObjectResult(errors);

                return;
            }
        }

        // Continue to the controller action
        await next();
    }
}