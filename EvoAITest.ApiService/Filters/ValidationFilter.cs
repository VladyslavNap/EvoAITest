using System.ComponentModel.DataAnnotations;
using EvoAITest.ApiService.Models;

namespace EvoAITest.ApiService.Filters;

/// <summary>
/// Endpoint filter that validates request models using DataAnnotations.
/// </summary>
/// <typeparam name="T">The type of the request model to validate.</typeparam>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    /// <summary>
    /// Validates the request model and returns a 400 Bad Request if validation fails.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Find the argument of type T in the endpoint parameters
        var argument = context.Arguments.FirstOrDefault(arg => arg is T) as T;

        if (argument is null)
        {
            // No argument of type T found, proceed to the next filter/handler
            return await next(context);
        }

        // Validate using DataAnnotations
        var validationContext = new ValidationContext(argument);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
        {
            // Build error response with validation errors
            var errors = validationResults
                .Where(r => r.ErrorMessage != null)
                .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "General")
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(r => r.ErrorMessage!).ToArray()
                );

            return Results.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                Code = "VALIDATION_ERROR",
                Errors = errors
            });
        }

        return await next(context);
    }
}
