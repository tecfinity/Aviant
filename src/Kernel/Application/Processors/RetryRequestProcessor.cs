using Aviant.Core.Services;
using MediatR;
using Polly;

namespace Aviant.Application.Processors;

public sealed class RetryRequestProcessor<TRequest, TResponse>
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse> _inner;

    private readonly IAsyncPolicy? _retryPolicy;

    public RetryRequestProcessor(IRequestHandler<TRequest, TResponse> inner)
    {
        _inner = inner;

        if (_inner is IRetry handler)
            _retryPolicy = handler.RetryPolicy();
    }

    #region IRequestHandler<TRequest,TResponse> Members

    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        // A handler that does not implement IRetry has no policy and is called directly.
        return _retryPolicy is null
            ? _inner.Handle(request, cancellationToken)
            : _retryPolicy.ExecuteAsync(() => _inner.Handle(request, cancellationToken));
    }

    #endregion
}
