namespace Aviant.Application.Email;

/// <summary>
///     An email address with an optional display name.
/// </summary>
public sealed record Mailbox(string Address, string? Name = null);

/// <summary>
///     A file sent with a message.
/// </summary>
/// <param name="FileName">The name the recipient sees.</param>
/// <param name="ContentType">The MIME type, e.g. <c>application/pdf</c>.</param>
/// <param name="Content">The file's bytes.</param>
public sealed record EmailAttachment(string FileName, string ContentType, ReadOnlyMemory<byte> Content)
{
    /// <summary>
    ///     Reads a file from disk as an attachment.
    /// </summary>
    public static async Task<EmailAttachment> FromFileAsync(
        string            path,
        string            contentType       = "application/octet-stream",
        CancellationToken cancellationToken = default) =>
        new(Path.GetFileName(path), contentType, await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false));
}

/// <summary>
///     A message to send. Immutable: derive variations with <c>with</c>.
/// </summary>
public sealed record EmailMessage
{
    /// <summary>
    ///     The sender. When omitted, the sender configured for the transport is used.
    /// </summary>
    public Mailbox? From { get; init; }

    public IReadOnlyList<Mailbox> To { get; init; } = [];

    public IReadOnlyList<Mailbox> Cc { get; init; } = [];

    public IReadOnlyList<Mailbox> Bcc { get; init; } = [];

    public IReadOnlyList<Mailbox> ReplyTo { get; init; } = [];

    public string Subject { get; init; } = string.Empty;

    /// <summary>
    ///     The HTML body. A message needs this, <see cref="TextBody" />, or both.
    /// </summary>
    public string? HtmlBody { get; init; }

    /// <summary>
    ///     The plain-text body, shown by clients that do not render HTML.
    /// </summary>
    public string? TextBody { get; init; }

    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];
}
