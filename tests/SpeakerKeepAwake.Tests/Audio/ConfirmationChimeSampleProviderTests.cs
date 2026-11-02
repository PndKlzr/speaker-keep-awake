using SpeakerKeepAwake.Audio;

namespace SpeakerKeepAwake.Tests.Audio;

public sealed class ConfirmationChimeSampleProviderTests
{
    [Fact]
    public void ProducesAnAudibleStereoChimeThenSilence()
    {
        const int sampleRate = 48_000;
        var provider = new ConfirmationChimeSampleProvider(sampleRate, channels: 2);
        var buffer = new float[sampleRate * 2];

        provider.Read(buffer, 0, buffer.Length);

        Assert.Contains(buffer, sample => Math.Abs(sample) >= 0.05f);
        for (var index = 0; index < buffer.Length; index += 2)
            Assert.Equal(buffer[index], buffer[index + 1]);
        Assert.All(buffer[^4..], sample => Assert.Equal(0f, sample));
    }
}
