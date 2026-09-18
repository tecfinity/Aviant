using MediatR;
using Microsoft.Extensions.Logging;

namespace Aviant.Application.Behaviours;

/// <summary>
///     Logs an exception that escapes a request's handler, then rethrows it.
/// </summary>
public sealed partial class UnhandledExceptionBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger _logger;

    public UnhandledExceptionBehaviour(ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger) =>
        _logger = logger;

    #region IPipelineBehavior<TRequest,TResponse> Members

    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 cancellationToken)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUnhandled(_logger, ex, typeof(TRequest).Name);

            throw;
        }
    }

    #endregion

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for request {Name}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string name);
}
