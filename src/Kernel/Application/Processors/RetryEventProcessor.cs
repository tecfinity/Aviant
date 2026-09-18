using Aviant.Core.Services;
using MediatR;
using Polly;

namespace Aviant.Application.Processors;

public sealed class RetryEventProcessor<TNotification>
    : INotificationHandler<TNotification>
    where TNotification : INotification
{
    private readonly INotificationHandler<TNotification> _inner;

    private readonly IAsyncPolicy? _retryPolicy;

    public RetryEventProcessor(INotificationHandler<TNotification> inner)
    {
        _inner = inner;

        if (_inner is IRetry handler)
            _retryPolicy = handler.RetryPolicy();
    }

    #region INotificationHandler<TNotification> Members

    public Task Handle(TNotification notification, CancellationToken cancellationToken)
    {
        // A handler that does not implement IRetry has no policy and is called directly.
        return _retryPolicy is null
            ? _inner.Handle(notification, cancellationToken)
            : _retryPolicy.ExecuteAsync(() => _inner.Handle(notification, cancellationToken));
    }

    #endregion
}
