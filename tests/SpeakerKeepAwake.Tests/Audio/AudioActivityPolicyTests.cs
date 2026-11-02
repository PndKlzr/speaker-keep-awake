using SpeakerKeepAwake.Audio;

namespace SpeakerKeepAwake.Tests.Audio;

public sealed class AudioActivityPolicyTests
{
    [Fact]
    public void IgnoresOnlyCurrentProcessSession()
    {
        AudioSessionSample[] sessions =
        [
            new(42, true, 0.8f),
            new(7, true, 0.02f),
            new(0, false, 0f)
        ];

        var active = AudioActivityPolicy.AnyOtherSessionAudible(sessions, currentProcessId: 42, threshold: 0.0005f);

        Assert.True(active);
    }

    [Fact]
    public void OwnSignalAloneDoesNotCountAsMediaSession()
    {
        AudioSessionSample[] sessions = [new(42, true, 0.8f), new(7, false, 0f)];

        var active = AudioActivityPolicy.AnyOtherSessionAudible(sessions, currentProcessId: 42, threshold: 0.0005f);

        Assert.False(active);
    }

    [Fact]
    public void ActiveButSilentMediaSessionDoesNotCountAsRealAudio()
    {
        AudioSessionSample[] sessions = [new(7, true, 0f)];

        var active = AudioActivityPolicy.AnyOtherSessionAudible(sessions, currentProcessId: 42, threshold: 0.0005f);

        Assert.False(active);
    }

    [Fact]
    public void InactiveSessionDoesNotCountEvenWithStalePeak()
    {
        AudioSessionSample[] sessions = [new(7, false, 0.8f)];

        var active = AudioActivityPolicy.AnyOtherSessionAudible(sessions, currentProcessId: 42, threshold: 0.0005f);

        Assert.False(active);
    }
}
