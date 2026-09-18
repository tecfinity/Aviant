using Microsoft.AspNetCore.Http;

namespace Aviant.Presentation.AspNetCore;

/// <summary>
///     Extra exception-to-status mappings for <see cref="AviantExceptionHandler" />.
/// </summary>
public sealed class AviantProblemDetailsOptions
{
    internal List<(Type Exception, int Status, string Title)> Mappings { get; } = [];

    /// <summary>
    ///     Answers <typeparamref name="TException" />, and exceptions derived from it, with <paramref name="status" />.
    ///     The exception's message becomes the detail.
    /// </summary>
    public AviantProblemDetailsOptions Map<TException>(int status, string title)
        where TException : Exception
    {
        if (status is < StatusCodes.Status400BadRequest or > 599)
            throw new ArgumentOutOfRangeException(nameof(status), status, "Map exceptions to 4xx or 5xx statuses.");

        Mappings.Add((typeof(TException), status, title));

        return this;
    }
}
