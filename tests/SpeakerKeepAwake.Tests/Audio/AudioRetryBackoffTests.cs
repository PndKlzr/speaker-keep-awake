using SpeakerKeepAwake.Audio;

namespace SpeakerKeepAwake.Tests.Audio;

public sealed class AudioRetryBackoffTests
{
    [Fact]
    public void FailureBlocksRetriesUntilDelayExpires()
    {
        var start = DateTimeOffset.Parse("2026-09-30T12:00:00Z");
        var backoff = new AudioRetryBackoff(TimeSpan.FromSeconds(5));

        backoff.RecordFailure(start);

        Assert.False(backoff.CanAttempt(start.AddSeconds(4)));
        Assert.True(backoff.CanAttempt(start.AddSeconds(5)));
    }

    [Fact]
    public void SuccessClearsBackoff()
    {
        var start = DateTimeOffset.Parse("2026-09-30T12:00:00Z");
        var backoff = new AudioRetryBackoff(TimeSpan.FromSeconds(5));
        backoff.RecordFailure(start);

        backoff.RecordSuccess();

        Assert.True(backoff.CanAttempt(start));
    }
}
