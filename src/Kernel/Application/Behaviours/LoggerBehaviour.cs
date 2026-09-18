using Aviant.Application.Processors;
using Microsoft.Extensions.Logging;

namespace Aviant.Application.Behaviours;

/// <summary>
///     Logs the name of every request as it enters the pipeline. The request's contents are not logged:
///     commands carry passwords, tokens and personal data.
/// </summary>
public partial class LoggerBehaviour<TRequest> : RequestPreProcessor<TRequest>
    where TRequest : notnull
{
    protected readonly ILogger Logger;

    public LoggerBehaviour(ILogger<LoggerBehaviour<TRequest>> logger) => Logger = logger;

    #region IRequestPreProcessor<TRequest> Members

    public override Task Process(TRequest request, CancellationToken cancellationToken)
    {
        LogRequest(Logger, typeof(TRequest).Name);

        return Task.CompletedTask;
    }

    #endregion

    [LoggerMessage(Level = LogLevel.Information, Message = "Request: {Name}")]
    private static partial void LogRequest(ILogger logger, string name);
}
