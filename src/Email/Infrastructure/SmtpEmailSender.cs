using System.Net.Sockets;
using Aviant.Application.Email;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace Aviant.Infrastructure.Email;

/// <summary>
///     Sends each message over its own SMTP connection, opened for the send and closed after it.
/// </summary>
public sealed partial class SmtpEmailSender(IEmailSettingsSource settingsSource, ILogger<SmtpEmailSender> logger)
    : IEmailSender
{
    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = await settingsSource.GetSmtpSettingsAsync(cancellationToken).ConfigureAwait(false);
        using var mime   = MimeMessageFactory.Create(message, settings.From);
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(settings.Host, settings.Port, ToSocketOptions(settings.Security), cancellationToken)
               .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(settings.Username))
                await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken)
                   .ConfigureAwait(false);

            await client.SendAsync(mime, cancellationToken).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is SocketException or IOException or ProtocolException
                                              or CommandException or AuthenticationException or SslHandshakeException)
        {
            LogFailed(logger, exception, settings.Host, settings.Port);

            throw new EmailDeliveryException($"The SMTP server {settings.Host}:{settings.Port} did not accept the message.", exception);
        }
    }

    private static SecureSocketOptions ToSocketOptions(SmtpSecurity security) => security switch
    {
        SmtpSecurity.None         => SecureSocketOptions.None,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        SmtpSecurity.StartTls     => SecureSocketOptions.StartTls,
        _                         => SecureSocketOptions.Auto
    };

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sending through {Host}:{Port} failed")]
    private static partial void LogFailed(ILogger logger, Exception exception, string host, int port);
}
