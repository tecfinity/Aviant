using Aviant.Core.Services;
using MediatR;

namespace Aviant.Application.Commands;

internal interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, TResponse>,
        IRetry
    where TCommand : ICommand<TResponse>;

internal interface ICommandHandler<in TCommand>
    : IRequestHandler<TCommand, Unit>,
        IRetry
    where TCommand : ICommand<Unit>;
