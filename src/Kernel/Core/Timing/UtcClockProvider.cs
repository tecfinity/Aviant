namespace Aviant.Core.Timing;

/// <inheritdoc />
/// <summary>
///     Implements <see cref="T:Aviant.Core.Timing.IClockProvider" /> to work with UTC times.
/// </summary>
public sealed class UtcClockProvider : IClockProvider
{
    internal UtcClockProvider()
    { }

    #region IClockProvider Members

    public DateTime Now => Clock.TimeProvider.GetUtcNow().UtcDateTime;

    public DateTimeKind Kind => DateTimeKind.Utc;

    public bool SupportsMultipleTimezone => true;

    public DateTime Normalize(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            DateTimeKind.Local       => dateTime.ToUniversalTime(),
            _                        => dateTime
        };
    }

    #endregion
}
