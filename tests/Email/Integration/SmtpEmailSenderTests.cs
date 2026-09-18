using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Aviant.Application.Email;
using Aviant.Infrastructure.Email;
using AwesomeAssertions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aviant.Tests.Email.Integration;

/// <summary>
///     Sends through a real SMTP server (Mailpit) and reads back what it received.
/// </summary>
public sealed class SmtpEmailSenderTests : IAsyncLifetime
{
    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.27")
       .WithPortBinding(1025, assignRandomHostPort: true)
       .WithPortBinding(8025, assignRandomHostPort: true)
       .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(8025).ForPath("/livez")))
       .Build();

    private readonly HttpClient _api = new();

    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _mailpit.StartAsync(TestContext.Current.CancellationToken);
        _api.BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(8025)}/api/v1/");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAviantEmail(new SmtpSettings
        {
            Host     = _mailpit.Hostname,
            Port     = _mailpit.GetMappedPublicPort(1025),
            Security = SmtpSecurity.None,
            From     = new Mailbox("no-reply@example.com", "Example")
        });
        _services = services.BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        _api.Dispose();
        await _services.DisposeAsync();
        await _mailpit.DisposeAsync();
    }

    [Fact]
    public async Task AMessageReachesEveryRecipientWithItsAttachment()
    {
        await using var scope = _services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        await sender.SendAsync(
            new EmailMessage
            {
                To          = [new("ada@example.com", "Ada"), new("grace@example.com")],
                Cc          = [new("team@example.com")],
                Bcc         = [new("audit@example.com")],
                Subject     = "Invoice 42",
                HtmlBody    = "<p>Attached.</p>",
                Attachments = [new EmailAttachment("invoice.txt", "text/plain", Encoding.UTF8.GetBytes("total: 42"))]
            },
            TestContext.Current.CancellationToken);

        var summary = (await _api.GetFromJsonAsync<Messages>("messages", TestContext.Current.CancellationToken))!.Items.Single();
        summary.Subject.Should().Be("Invoice 42");
        summary.From.Address.Should().Be("no-reply@example.com");
        summary.To.Select(m => m.Address).Should().Equal("ada@example.com", "grace@example.com");
        summary.Cc.Select(m => m.Address).Should().Equal("team@example.com");
        summary.Bcc.Select(m => m.Address).Should().Equal("audit@example.com");
        summary.Attachments.Should().Be(1);
    }

    [Fact]
    public async Task AnUnreachableServerIsADeliveryFailure()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAviantEmail(new SmtpSettings { Host = "127.0.0.1", Port = 1, Security = SmtpSecurity.None, From = new Mailbox("no-reply@example.com") });
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var act = () => scope.ServiceProvider.GetRequiredService<IEmailSender>().SendAsync(
            new EmailMessage { To = [new("ada@example.com")], Subject = "Hi", TextBody = "Hi" },
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<EmailDeliveryException>();
    }

    private sealed record Messages([property: JsonPropertyName("messages")] List<Summary> Items);

    private sealed record Summary(
        string Subject,
        Recipient From,
        List<Recipient> To,
        List<Recipient> Cc,
        List<Recipient> Bcc,
        int Attachments);

    private sealed record Recipient(string Address);
}
