using Aviant.Application.ApplicationEvents;
using Aviant.Application.Commands;
using Aviant.Application.Queries;
using Aviant.Core.Messages;
using MediatR;
using Microsoft.Extensions.Options;

namespace Aviant.Application.Orchestration;

public abstract class OrchestratorBase
{
    private readonly IApplicationEventDispatcher _applicationEventDispatcher;

    private readonly IMediator _mediator;

    private readonly IMessages _messages;

    private readonly OrchestratorOptions _options;


    protected OrchestratorBase(
        IMessages                   messages,
        IApplicationEventDispatcher applicationEventDispatcher,
        IMediator                   mediator)
        : this(messages, applicationEventDispatcher, mediator, Options.Create(new OrchestratorOptions()))
    { }

    protected OrchestratorBase(
        IMessages                     messages,
        IApplicationEventDispatcher   applicationEventDispatcher,
        IMediator                     mediator,
        IOptions<OrchestratorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _messages                   = messages;
        _applicationEventDispatcher = applicationEventDispatcher;
        _mediator                   = mediator;
        _options                    = options.Value;
    }

    /// <summary>
    ///     Whether <paramref name="exception" /> is a domain refusal, to be reported to the
    ///     caller as a failed response rather than propagated.
    /// </summary>
    protected bool IsRefusal(Exception exception) => _options.IsRefusal(exception);

    protected async Task<(TCommandResponse commandResponse, List<string>? _messages)>
        PreUnitOfWorkAsync<TCommand, TCommandResponse>(
            TCommand          command,
            CancellationToken cancellationToken = default)
        where TCommand : class, IRequest<TCommandResponse>
    {
        TCommandResponse commandResponse;

        try
        {
            commandResponse = await _mediator.Send(command, cancellationToken)
               .ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRefusal(exception))
        {
            return (default!, [exception.Message]);
        }

        // Fire pre/post notifications
        await _applicationEventDispatcher.FirePreCommitEventsAsync(cancellationToken)
           .ConfigureAwait(false);

        List<string>? messages = null;

        if (_messages.HasMessages())
            messages = _messages.GetAll();

        return (commandResponse, messages);
    }

    protected async Task<dynamic?> PostUnitOfWorkAsync<TCommandResponse>(
        TCommandResponse  commandResponse,
        CancellationToken cancellationToken)
    {
        // Fire post commit notifications
        await _applicationEventDispatcher.FirePostCommitEventsAsync(cancellationToken)
           .ConfigureAwait(false);

        var isLazy = false;

        try
        {
            isLazy = commandResponse is not null
                  && commandResponse.GetType().IsGenericType //TODO: Revisit this check, maybe it shouldn't be here
                  && typeof(Lazy<>) == commandResponse.GetType().GetGenericTypeDefinition();
        }
        catch (Exception)
        {
            // ignore
        }

        return isLazy
            ? commandResponse?.GetType().GetProperty("Value")?.GetValue(commandResponse, null)
            : commandResponse;
    }

    public async Task<OrchestratorResponse> SendQueryAsync<T>(
        IQuery<T>         query,
        CancellationToken cancellationToken = default)
    {
        T queryResponse;

        try
        {
            queryResponse = await _mediator.Send(query, cancellationToken)
               .ConfigureAwait(false);
        }
        catch (Exception exception) when (IsRefusal(exception))
        {
            return new OrchestratorResponse([exception.Message]);
        }

        return _messages.HasMessages()
            ? new OrchestratorResponse(_messages.GetAll())
            : new OrchestratorResponse(queryResponse);
    }
}

public sealed class Orchestrator
    : OrchestratorBase,
      IOrchestrator
{
    public Orchestrator(
        IMessages                   messages,
        IApplicationEventDispatcher applicationEventDispatcher,
        IMediator                   mediator)
        : base(messages, applicationEventDispatcher, mediator)
    { }

    public Orchestrator(
        IMessages                     messages,
        IApplicationEventDispatcher   applicationEventDispatcher,
        IMediator                     mediator,
        IOptions<OrchestratorOptions> options)
        : base(messages, applicationEventDispatcher, mediator, options)
    { }

    #region IOrchestrator Members

    public async Task<OrchestratorResponse> SendCommandAsync<T>(
        ICommand<T>       command,
        CancellationToken cancellationToken = default)
    {
        (var commandResponse, List<string>? messages) = await PreUnitOfWorkAsync<ICommand<T>, T>(
                command,
                cancellationToken)
           .ConfigureAwait(false);

        if (messages is not null)
            return new OrchestratorResponse(messages);

        var result = await PostUnitOfWorkAsync(commandResponse, cancellationToken)
           .ConfigureAwait(false);

        return new OrchestratorResponse(result);
    }

    #endregion
}
