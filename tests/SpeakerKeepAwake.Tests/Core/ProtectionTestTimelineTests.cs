using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Tests.Core;

public sealed class ProtectionTestTimelineTests
{
    [Fact]
    public void KeepsSignalRunningBeforeAudibleConfirmation()
    {
        var startedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        var timeline = new ProtectionTestTimeline(startedAt);

        var state = timeline.GetState(startedAt.AddSeconds(5));

        Assert.Equal(ProtectionTestPhase.KeepAlive, state.Phase);
        Assert.Equal(3, state.SecondsRemaining);
    }

    [Fact]
    public void RequestsAudibleConfirmationAfterKeepAliveWindow()
    {
        var startedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        var timeline = new ProtectionTestTimeline(startedAt);

        var state = timeline.GetState(startedAt.AddSeconds(8));

        Assert.Equal(ProtectionTestPhase.Confirmation, state.Phase);
        Assert.Equal(0, state.SecondsRemaining);
    }

    [Fact]
    public void CompletesAfterConfirmationHasPlayed()
    {
        var startedAt = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        var timeline = new ProtectionTestTimeline(startedAt);

        var state = timeline.GetState(startedAt.AddMilliseconds(9_200));

        Assert.Equal(ProtectionTestPhase.Complete, state.Phase);
    }
}
