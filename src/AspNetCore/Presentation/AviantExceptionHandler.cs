using Aviant.Application.Exceptions;
using Aviant.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aviant.Presentation.AspNetCore;

/// <summary>
///     Answers Aviant's exceptions with RFC 9457 problem details: a validation failure is a 400 listing the errors, a
///     missing resource a 404, a refused domain rule a 400 with its message.
/// </summary>
/// <remarks>
///     Any other exception is left to the next handler, and ASP.NET Core's default then answers it with a 500
///     problem details. Register an <see cref="IExceptionHandler" /> before this one to observe every exception,
///     for example to record it, and return <see langword="false" /> from it.
/// </remarks>
public sealed class AviantExceptionHandler(
    IProblemDetailsService                problemDetails,
    IOptions<AviantProblemDetailsOptions> options) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext       httpContext,
        Exception         exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (Describe(exception) is not { } details)
            return false;

        details.Instance ??= httpContext.Request.Path;
        httpContext.Response.StatusCode = details.Status!.Value;

        return await problemDetails.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext    = httpContext,
                    ProblemDetails = details,
                    Exception      = exception
                })
           .ConfigureAwait(false);
    }

    private ProblemDetails? Describe(Exception exception)
    {
        foreach (var (type, status, title) in options.Value.Mappings)
            if (type.IsInstanceOfType(exception))
                return new ProblemDetails { Status = status, Title = title, Detail = exception.Message };

        return exception switch
        {
            ValidationException validation => new ValidationProblemDetails(
                validation.Failures ?? new Dictionary<string, string[]>())
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "One or more validation failures have occurred."
            },
            NotFoundException or EntityNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title  = "The specified resource was not found.",
                Detail = exception.Message
            },
            DomainRuleException => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title  = "The request was refused.",
                Detail = exception.Message
            },
            _ => null
        };
    }
}
