using NAudio.Wave;
using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Audio;

public static class SignalLevels
{
    public const float HardMaximumAmplitude = 0.01f;

    public static float GetAmplitude(SignalLevel level) => level switch
    {
        SignalLevel.Minimum => 0.001f,
        SignalLevel.Low => 0.003162f,
        SignalLevel.Medium => 0.005623f,
        SignalLevel.High => HardMaximumAmplitude,
        _ => 0.001f
    };
}

public sealed class KeepAliveSampleProvider : ISampleProvider
{
    private const double PreferredFrequency = 18_000d;
    private readonly int _channels;
    private readonly float _amplitude;
    private readonly double _phaseIncrement;
    private double _phase;
    private int _channelIndex;
    private float _currentSample;

    public KeepAliveSampleProvider(int sampleRate, int channels, float amplitude)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));

        _channels = channels;
        _amplitude = Math.Clamp(amplitude, 0f, SignalLevels.HardMaximumAmplitude);
        var frequency = Math.Min(PreferredFrequency, sampleRate * 0.45d);
        _phaseIncrement = 2d * Math.PI * frequency / sampleRate;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public int Read(Span<float> buffer)
    {
        for (var index = 0; index < buffer.Length; index++)
        {
            if (_channelIndex == 0)
            {
                _currentSample = (float)(Math.Sin(_phase) * _amplitude);
            }

            buffer[index] = _currentSample;
            _channelIndex++;
            if (_channelIndex == _channels)
            {
                _channelIndex = 0;
                _phase += _phaseIncrement;
                if (_phase >= 2d * Math.PI) _phase -= 2d * Math.PI;
            }
        }

        return buffer.Length;
    }
}
