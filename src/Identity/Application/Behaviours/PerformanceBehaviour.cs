using MediatR;
using Microsoft.Extensions.Logging;

namespace Aviant.Application.Identity.Behaviours;

/// <summary>
///     Warns about slow requests, naming the user who made them.
/// </summary>
public partial class PerformanceBehaviour<TRequest, TResponse>
    : Application.Behaviours.PerformanceBehaviour<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUserService;

    private readonly IIdentityService _identityService;

    public PerformanceBehaviour(
        ICurrentUserService                                              currentUserService,
        IIdentityService                                                 identityService,
        ILogger<Application.Behaviours.PerformanceBehaviour<TRequest, TResponse>> logger)
        : base(logger)
    {
        _currentUserService = currentUserService;
        _identityService    = identityService;
    }

    protected override async Task OnLongRunningAsync(
        TRequest          request,
        long              elapsedMilliseconds,
        CancellationToken cancellationToken)
    {
        var userId   = _currentUserService.UserId;
        var userName = Guid.Empty == userId
            ? string.Empty
            : await _identityService.GetUserNameAsync(userId, cancellationToken).ConfigureAwait(false);

        LogLongRunning(Logger, typeof(TRequest).Name, elapsedMilliseconds, userId, userName);
    }

    [LoggerMessage(
        Level   = LogLevel.Warning,
        Message = "Long running request: {Name} ({ElapsedMilliseconds} milliseconds), UserId: {UserId}, UserName: {UserName}")]
    private static partial void LogLongRunning(ILogger logger, string name, long elapsedMilliseconds, Guid userId, string userName);
}
