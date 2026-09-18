namespace Aviant.Application.Email;

/// <summary>
///     Delivers messages. SMTP is built in (<c>AddAviantEmail</c>); register another implementation to send through
///     an HTTP provider instead.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    ///     Sends <paramref name="message" />.
    /// </summary>
    /// <exception cref="EmailDeliveryException">The message could not be delivered to the transport.</exception>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
///     A message could not be handed to the mail transport.
/// </summary>
public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException()
    { }

    public EmailDeliveryException(string message)
        : base(message)
    { }

    public EmailDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
