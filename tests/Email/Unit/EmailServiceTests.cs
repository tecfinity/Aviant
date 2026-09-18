using Aviant.Application.Email;
using Aviant.Infrastructure.Email;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aviant.Tests.Email.Unit;

public sealed class EmailServiceTests
{
    [Fact]
    public async Task TheFluentServiceSendsOneMessageToEveryRecipientItWasGiven()
    {
        var sender  = new RecordingSender();
        var service = new EmailService(sender, NullLogger<EmailService>.Instance);

        var sent = await service
           .To("Ada", "ada@example.com")
           .To("Grace", "grace@example.com")
           .WithSubject("Hello")
           .WithBodyHtml("<p>Hi</p>")
           .SendAsync(TestContext.Current.CancellationToken);

        sent.Should().BeTrue();
        sender.Sent.Should().ContainSingle().Which.To.Select(m => m.Address)
           .Should().Equal("ada@example.com", "grace@example.com");
    }

    [Fact]
    public async Task EachSendStartsAFreshMessage()
    {
        var sender  = new RecordingSender();
        var service = new EmailService(sender, NullLogger<EmailService>.Instance);

        await service.To("Ada", "ada@example.com").WithSubject("One").WithBodyPlain("1").SendAsync(TestContext.Current.CancellationToken);
        await service.To("Grace", "grace@example.com").WithSubject("Two").WithBodyPlain("2").SendAsync(TestContext.Current.CancellationToken);

        sender.Sent[1].To.Select(m => m.Address).Should().Equal("grace@example.com");
        sender.Sent[1].Subject.Should().Be("Two");
    }

    [Fact]
    public async Task AFailedDeliveryIsReportedAsFalse()
    {
        var service = new EmailService(new FailingSender(), NullLogger<EmailService>.Instance);

        var sent = await service.To("Ada", "ada@example.com").WithSubject("Hi").WithBodyPlain("Hi")
           .SendAsync(TestContext.Current.CancellationToken);

        sent.Should().BeFalse();
    }

    [Fact]
    public void CreatingTheServiceDoesNotTouchTheNetwork()
    {
        var act = () => new EmailService(new FailingSender(), NullLogger<EmailService>.Instance);

        act.Should().NotThrow();
    }

    private sealed class RecordingSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            Sent.Add(message);

            return Task.CompletedTask;
        }
    }

    private sealed class FailingSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default) =>
            throw new EmailDeliveryException("The relay refused the message.");
    }
}
