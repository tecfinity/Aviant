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
