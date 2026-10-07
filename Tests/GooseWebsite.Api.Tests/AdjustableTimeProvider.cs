namespace GooseWebsite.Api.Tests;

// Lets rate-limit tests advance expiry windows without waiting on wall-clock time.
internal sealed class AdjustableTimeProvider(DateTimeOffset currentTime) : TimeProvider
{
    private DateTimeOffset _currentTime = currentTime;

    public override DateTimeOffset GetUtcNow() => _currentTime;

    public void Advance(TimeSpan duration) => _currentTime += duration;
}