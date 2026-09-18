using Aviant.Core.Timing;
using AwesomeAssertions;
using Xunit;

namespace Aviant.Tests.Kernel.Unit.Timing;

// Clock is process-wide; these tests must not run alongside others that replace it.
[Collection(nameof(ClockTests))]
public sealed class ClockTests
{
    private static readonly DateTimeOffset Instant = new(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    public void ClockReadsTheTimeProviderItIsGiven()
    {
        using var _ = UseTime(Instant);

        ClockProviders.Utc.Now.Should().Be(Instant.UtcDateTime);
        ClockProviders.Utc.Now.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void TheCurrentProviderUsesTheTimeProviderToo()
    {
        using var _ = UseTime(Instant);
        var previous = Clock.Provider;
        Clock.Provider = ClockProviders.Utc;

        try
        {
            Clock.Now.Should().Be(Instant.UtcDateTime);
        }
        finally
        {
            Clock.Provider = previous;
        }
    }

    [Fact]
    public void LocalTimeFollowsTheTimeProvidersZone()
    {
        using var _ = UseTime(Instant, TimeZoneInfo.CreateCustomTimeZone("plus-two", TimeSpan.FromHours(2), "plus-two", "plus-two"));

        ClockProviders.Local.Now.Should().Be(new DateTime(2030, 1, 2, 5, 4, 5));
        ClockProviders.Local.Now.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void ANullTimeProviderIsRefused()
    {
        var act = () => Clock.TimeProvider = null!;

        act.Should().Throw<ArgumentNullException>();
    }

    private static IDisposable UseTime(DateTimeOffset now, TimeZoneInfo? zone = null)
    {
        var previous = Clock.TimeProvider;
        Clock.TimeProvider = new FixedTimeProvider(now, zone ?? TimeZoneInfo.Utc);

        return new Restore(() => Clock.TimeProvider = previous);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now, TimeZoneInfo zone) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => zone;
    }

    private sealed class Restore(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
