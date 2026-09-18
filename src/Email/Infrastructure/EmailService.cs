using Aviant.Application.Email;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.Email;

/// <inheritdoc />
public sealed partial class EmailService(IEmailSender sender, ILogger<EmailService> logger) : IEmailService
{
    private EmailMessage _message = new();

    /// <inheritdoc />
    public IEmailService From(string name, string address)
    {
        _message = _message with { From = new Mailbox(address, name) };

        return this;
    }

    /// <inheritdoc />
    public IEmailService To(string name, string address)
    {
        _message = _message with { To = [.. _message.To, new Mailbox(address, name)] };

        return this;
    }

    /// <inheritdoc />
    public IEmailService WithSubject(string subject)
    {
        _message = _message with { Subject = subject };

        return this;
    }

    /// <inheritdoc />
    public IEmailService WithBodyHtml(string body)
    {
        _message = _message with { HtmlBody = body };

        return this;
    }

    /// <inheritdoc />
    public IEmailService WithBodyPlain(string body)
    {
        _message = _message with { TextBody = body };

        return this;
    }

    /// <inheritdoc />
    public IEmailService Attach(EmailAttachment attachment)
    {
        _message = _message with { Attachments = [.. _message.Attachments, attachment] };

        return this;
    }

    /// <inheritdoc />
    public async Task<bool> SendAsync(CancellationToken cancellationToken = default)
    {
        var message = _message;
        Message();

        try
        {
            await sender.SendAsync(message, cancellationToken).ConfigureAwait(false);

            return true;
        }
        catch (EmailDeliveryException exception)
        {
            LogNotSent(logger, exception, message.Subject);

            return false;
        }
    }

    /// <inheritdoc />
    public IEmailService Message()
    {
        _message = new EmailMessage();

        return this;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The message \"{Subject}\" was not sent")]
    private static partial void LogNotSent(ILogger logger, Exception exception, string subject);
}
