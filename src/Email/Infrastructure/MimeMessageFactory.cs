using Aviant.Application.Email;
using MimeKit;

namespace Aviant.Infrastructure.Email;

/// <summary>
///     Builds the MIME message for an <see cref="EmailMessage" />; useful for transports that take raw MIME.
/// </summary>
public static class MimeMessageFactory
{
    /// <param name="message">The message to package.</param>
    /// <param name="defaultFrom">The sender when the message names none.</param>
    /// <exception cref="ArgumentException">The message has no recipient, no sender or no body.</exception>
    public static MimeMessage Create(EmailMessage message, Mailbox? defaultFrom)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.To.Count + message.Cc.Count + message.Bcc.Count == 0)
            throw new ArgumentException("A message needs at least one recipient.", nameof(message));

        if (string.IsNullOrEmpty(message.HtmlBody) && string.IsNullOrEmpty(message.TextBody))
            throw new ArgumentException("A message needs an HTML or a text body.", nameof(message));

        var from = message.From ?? defaultFrom
         ?? throw new ArgumentException("The message has no sender, and none is configured.", nameof(message));

        var mime = new MimeMessage { Subject = message.Subject };
        mime.From.Add(ToMailbox(from));
        mime.To.AddRange(message.To.Select(ToMailbox));
        mime.Cc.AddRange(message.Cc.Select(ToMailbox));
        mime.Bcc.AddRange(message.Bcc.Select(ToMailbox));
        mime.ReplyTo.AddRange(message.ReplyTo.Select(ToMailbox));

        var body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody };

        foreach (var attachment in message.Attachments)
            body.Attachments.Add(attachment.FileName, attachment.Content.ToArray(), ContentType.Parse(attachment.ContentType));

        mime.Body = body.ToMessageBody();

        return mime;
    }

    private static MailboxAddress ToMailbox(Mailbox mailbox) => new(mailbox.Name ?? string.Empty, mailbox.Address);
}
