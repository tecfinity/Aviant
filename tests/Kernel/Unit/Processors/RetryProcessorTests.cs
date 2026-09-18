using Aviant.Application.Processors;
using Aviant.Core.Services;
using AwesomeAssertions;
using MediatR;
using Polly;
using Xunit;

namespace Aviant.Tests.Kernel.Unit.Processors;

public sealed class RetryProcessorTests
{
    [Fact]
    public async Task RequestHandlerWithoutRetryPolicyIsCalledDirectly()
    {
        var processor = new RetryRequestProcessor<Ping, string>(new PlainPingHandler());

        var response = await processor.Handle(new Ping(), TestContext.Current.CancellationToken);

        response.Should().Be("pong");
    }

    [Fact]
    public async Task RequestHandlerWithRetryPolicyIsRetriedUntilItSucceeds()
    {
        var handler   = new FlakyPingHandler(failures: 2);
        var processor = new RetryRequestProcessor<Ping, string>(handler);

        var response = await processor.Handle(new Ping(), TestContext.Current.CancellationToken);

        response.Should().Be("pong");
        handler.Attempts.Should().Be(3);
    }

    [Fact]
    public async Task NotificationHandlerWithoutRetryPolicyIsCalledDirectly()
    {
        var handler   = new PlainNoticeHandler();
        var processor = new RetryEventProcessor<Notice>(handler);

        await processor.Handle(new Notice(), TestContext.Current.CancellationToken);

        handler.Handled.Should().BeTrue();
    }

    public sealed record Ping : IRequest<string>;

    public sealed record Notice : INotification;

    private sealed class PlainPingHandler : IRequestHandler<Ping, string>
    {
        public Task<string> Handle(Ping request, CancellationToken cancellationToken) =>
            Task.FromResult("pong");
    }

    private sealed class FlakyPingHandler(int failures) : IRequestHandler<Ping, string>, IRetry
    {
        public int Attempts { get; private set; }

        public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            Attempts++;

            return Attempts <= failures
                ? throw new TimeoutException("transient")
                : Task.FromResult("pong");
        }

        public IAsyncPolicy RetryPolicy() => Policy.Handle<TimeoutException>().RetryAsync(failures);
    }

    private sealed class PlainNoticeHandler : INotificationHandler<Notice>
    {
        public bool Handled { get; private set; }

        public Task Handle(Notice notification, CancellationToken cancellationToken)
        {
            Handled = true;

            return Task.CompletedTask;
        }
    }
}
