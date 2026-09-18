namespace Aviant.Application.Email;

/// <summary>
///     How the connection to the SMTP server is secured.
/// </summary>
public enum SmtpSecurity
{
    /// <summary>Use TLS if the server offers it.</summary>
    Auto,

    /// <summary>No encryption, as with a local relay or Mailpit.</summary>
    None,

    /// <summary>TLS from the first byte, usually port 465.</summary>
    SslOnConnect,

    /// <summary>Upgrade to TLS with STARTTLS, usually port 587.</summary>
    StartTls
}

/// <summary>
///     An SMTP server and the default sender for messages that name none.
/// </summary>
public sealed record SmtpSettings
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 25;

    public SmtpSecurity Security { get; init; } = SmtpSecurity.Auto;

    /// <summary>
    ///     The account to authenticate as; none for an anonymous relay.
    /// </summary>
    public string? Username { get; init; }

    public string? Password { get; init; }

    /// <summary>
    ///     The sender of messages that do not set <see cref="EmailMessage.From" />.
    /// </summary>
    public Mailbox? From { get; init; }
}

/// <summary>
///     Supplies the SMTP settings for the current scope, which may differ per tenant or site.
/// </summary>
public interface IEmailSettingsSource
{
    ValueTask<SmtpSettings> GetSmtpSettingsAsync(CancellationToken cancellationToken = default);
}
