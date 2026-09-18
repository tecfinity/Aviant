using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aviant.Application.Behaviours;

/// <summary>
///     Warns about requests that take longer than half a second.
/// </summary>
public partial class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const long ThresholdMilliseconds = 500;

    protected readonly ILogger Logger;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger) => Logger = logger;

    #region IPipelineBehavior<TRequest,TResponse> Members

    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 cancellationToken)
    {
        var started  = Stopwatch.GetTimestamp();
        var response = await next().ConfigureAwait(false);
        var elapsed  = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (elapsed > ThresholdMilliseconds)
            await OnLongRunningAsync(request, elapsed, cancellationToken).ConfigureAwait(false);

        return response;
    }

    #endregion

    /// <summary>
    ///     Called when a request took longer than the threshold. Logs the request's name and duration.
    /// </summary>
    protected virtual Task OnLongRunningAsync(TRequest request, long elapsedMilliseconds, CancellationToken cancellationToken)
    {
        LogLongRunning(Logger, typeof(TRequest).Name, elapsedMilliseconds);

        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Long running request: {Name} ({ElapsedMilliseconds} milliseconds)")]
    private static partial void LogLongRunning(ILogger logger, string name, long elapsedMilliseconds);
}
