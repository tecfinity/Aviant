using Microsoft.Extensions.Logging;

namespace Aviant.Application.Identity.Behaviours;

/// <summary>
///     Logs the name of every request and the id of the user who sent it. The request's contents are not logged.
/// </summary>
public partial class LoggerBehaviour<TRequest> : Application.Behaviours.LoggerBehaviour<TRequest>
    where TRequest : notnull
{
    private readonly ICurrentUserService _currentUserService;

    public LoggerBehaviour(
        ICurrentUserService                                        currentUserService,
        ILogger<Application.Behaviours.LoggerBehaviour<TRequest>> logger)
        : base(logger) =>
        _currentUserService = currentUserService;

    #region IRequestPreProcessor<TRequest> Members

    public override Task Process(TRequest request, CancellationToken cancellationToken)
    {
        LogRequest(Logger, typeof(TRequest).Name, _currentUserService.UserId);

        return Task.CompletedTask;
    }

    #endregion

    [LoggerMessage(Level = LogLevel.Information, Message = "Request: {Name}, UserId: {UserId}")]
    private static partial void LogRequest(ILogger logger, string name, Guid userId);
}
