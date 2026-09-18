namespace Aviant.Application.Email;

/// <summary>
///     A fluent way to compose one message at a time and send it through <see cref="IEmailSender" />.
/// </summary>
/// <remarks>
///     Each call to <see cref="To" /> adds a recipient. <see cref="SendAsync" /> sends the message and starts a new
///     one. It reports a failed delivery as <see langword="false" /> and logs it; use <see cref="IEmailSender" />
///     directly to handle failures yourself.
/// </remarks>
public interface IEmailService
{
    public IEmailService From(string name, string address);

    public IEmailService To(string name, string address);

    public IEmailService WithSubject(string subject);

    public IEmailService WithBodyHtml(string body);

    public IEmailService WithBodyPlain(string body);

    public IEmailService Attach(EmailAttachment attachment);

    public Task<bool> SendAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Discards the message being composed and starts a new one.
    /// </summary>
    public IEmailService Message();
}
