using System.Text;
using Aviant.Application.Email;
using Aviant.Infrastructure.Email;
using AwesomeAssertions;
using MimeKit;
using Xunit;

namespace Aviant.Tests.Email.Unit;

public sealed class MimeMessageFactoryTests
{
    private static readonly Mailbox Platform = new("no-reply@example.com", "Example");

    [Fact]
    public void EveryRecipientKindIsCarriedOver()
    {
        var message = new EmailMessage
        {
            To      = [new("ada@example.com", "Ada"), new("grace@example.com")],
            Cc      = [new("team@example.com")],
            Bcc     = [new("audit@example.com")],
            ReplyTo = [new("support@example.com")],
            Subject = "Welcome",
            TextBody = "Hello"
        };

        using var mime = MimeMessageFactory.Create(message, Platform);

        mime.To.Mailboxes.Select(m => m.Address).Should().Equal("ada@example.com", "grace@example.com");
        mime.To.Mailboxes.First().Name.Should().Be("Ada");
        mime.Cc.Mailboxes.Select(m => m.Address).Should().Equal("team@example.com");
        mime.Bcc.Mailboxes.Select(m => m.Address).Should().Equal("audit@example.com");
        mime.ReplyTo.Mailboxes.Select(m => m.Address).Should().Equal("support@example.com");
        mime.Subject.Should().Be("Welcome");
    }

    /// <remarks>
    ///  List-Unsubscribe is why this exists. The large mailbox providers have required it
    ///  of bulk senders since 2024, and a message without it is a message whose delivery
    ///  rate falls for a reason the sender cannot see.
    /// </remarks>
    [Fact]
    public void ExtraHeadersReachTheMessage()
    {
        var message = Minimal() with
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["List-Unsubscribe"]      = "<https://example.com/u?t=abc>",
                ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
            },
        };

        using var mime = MimeMessageFactory.Create(message, Platform);

        mime.Headers["List-Unsubscribe"].Should().Be("<https://example.com/u?t=abc>");
        mime.Headers["List-Unsubscribe-Post"].Should().Be("List-Unsubscribe=One-Click");
    }

    /// <remarks>
    ///  A header set twice can be read either way round by a receiver, and two different
    ///  List-Unsubscribe addresses is worse than none.
    /// </remarks>
    [Fact]
    public void AHeaderIsWrittenOnceHoweverItIsSpelled()
    {
        var message = Minimal() with
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["List-Unsubscribe"] = "<https://example.com/one>",
            },
        };

        using var mime = MimeMessageFactory.Create(message, Platform);

        mime.Headers.Count(header =>
                header.Field.Equals("List-Unsubscribe", StringComparison.OrdinalIgnoreCase))
           .Should()
           .Be(1);
    }

    /// <remarks>
    ///  An empty header is a header the sender did not mean to set. Writing it produces a
    ///  bare field name that some receivers treat as a malformed message.
    /// </remarks>
    [Fact]
    public void EmptyHeadersAreNotWritten()
    {
        var message = Minimal() with
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Nothing"] = "   ",
            },
        };

        using var mime = MimeMessageFactory.Create(message, Platform);

        mime.Headers.Contains("X-Nothing").Should().BeFalse();
    }

    [Fact]
    public void TheDefaultSenderIsUsedUnlessTheMessageNamesOne()
    {
        using var fromDefault = MimeMessageFactory.Create(Minimal(), Platform);
        using var fromOwn     = MimeMessageFactory.Create(Minimal() with { From = new("editor@example.com", "Editor") }, Platform);

        fromDefault.From.Mailboxes.Single().Address.Should().Be("no-reply@example.com");
        fromOwn.From.Mailboxes.Single().Address.Should().Be("editor@example.com");
    }

    [Fact]
    public void HtmlAndTextBodiesAndAttachmentsArePackaged()
    {
        var message = Minimal() with
        {
            HtmlBody    = "<p>Hello</p>",
            TextBody    = "Hello",
            Attachments = [new EmailAttachment("invoice.pdf", "application/pdf", Encoding.ASCII.GetBytes("%PDF-1.7"))]
        };

        using var mime = MimeMessageFactory.Create(message, Platform);

        mime.HtmlBody.Should().Be("<p>Hello</p>");
        mime.TextBody.Should().Be("Hello");
        var attachment = mime.Attachments.OfType<MimePart>().Single();
        attachment.FileName.Should().Be("invoice.pdf");
        attachment.ContentType.MimeType.Should().Be("application/pdf");
    }

    [Fact]
    public void AMessageWithNoRecipientIsRefused()
    {
        var act = () => MimeMessageFactory.Create(Minimal() with { To = [] }, Platform);

        act.Should().Throw<ArgumentException>().WithMessage("*recipient*");
    }

    [Fact]
    public void AMessageWithNoBodyIsRefused()
    {
        var act = () => MimeMessageFactory.Create(new EmailMessage { To = [new("ada@example.com")], Subject = "Empty" }, Platform);

        act.Should().Throw<ArgumentException>().WithMessage("*body*");
    }

    private static EmailMessage Minimal() =>
        new() { To = [new("ada@example.com")], Subject = "Hi", TextBody = "Hi" };
}
