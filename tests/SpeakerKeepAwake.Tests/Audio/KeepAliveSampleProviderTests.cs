using SpeakerKeepAwake.Audio;
using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Tests.Audio;

public sealed class KeepAliveSampleProviderTests
{
    [Fact]
    public void ProducesEighteenKilohertzToneIdenticallyOnBothChannels()
    {
        const float amplitude = 0.01f;
        var provider = new KeepAliveSampleProvider(sampleRate: 48_000, channels: 2, amplitude);
        var buffer = new float[8];

        var read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(buffer.Length, read);
        Assert.Equal(buffer[0], buffer[1]);
        Assert.Equal(buffer[2], buffer[3]);
        Assert.Equal(buffer[4], buffer[5]);
        Assert.Equal(buffer[6], buffer[7]);
        Assert.Equal(0f, buffer[0], precision: 6);
        Assert.Equal(0.007071f, buffer[2], precision: 6);
        Assert.Equal(-0.01f, buffer[4], precision: 6);
        Assert.Equal(0.007071f, buffer[6], precision: 6);
        Assert.All(buffer, sample => Assert.InRange(Math.Abs(sample), 0f, amplitude));
    }

    [Theory]
    [InlineData(SignalLevel.Minimum, 0.001f)]
    [InlineData(SignalLevel.Low, 0.003162f)]
    [InlineData(SignalLevel.Medium, 0.005623f)]
    [InlineData(SignalLevel.High, 0.01f)]
    public void SignalLevelsUseCalibratedAmplitudes(SignalLevel level, float expectedAmplitude)
    {
        var amplitude = SignalLevels.GetAmplitude(level);

        Assert.Equal(expectedAmplitude, amplitude, precision: 6);
        Assert.InRange(amplitude, 0f, SignalLevels.HardMaximumAmplitude);
    }
}
