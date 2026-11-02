using NAudio.Wave;

namespace SpeakerKeepAwake.Audio;

public sealed class ConfirmationChimeSampleProvider : ISampleProvider
{
    private const float Amplitude = 0.12f;
    private readonly int _sampleRate;
    private readonly int _channels;
    private long _frame;
    private int _channelIndex;
    private float _currentSample;

    public ConfirmationChimeSampleProvider(int sampleRate, int channels)
    {
        if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));

        _sampleRate = sampleRate;
        _channels = channels;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public int Read(Span<float> buffer)
    {
        for (var index = 0; index < buffer.Length; index++)
        {
            if (_channelIndex == 0) _currentSample = SampleAt(_frame++ / (double)_sampleRate);
            buffer[index] = _currentSample;
            _channelIndex = (_channelIndex + 1) % _channels;
        }

        return buffer.Length;
    }

    private static float SampleAt(double seconds)
    {
        if (seconds < 0.22d) return Note(seconds, 0d, 0.22d, 659.25d);
        if (seconds >= 0.28d && seconds < 0.62d) return Note(seconds, 0.28d, 0.62d, 880d);
        return 0f;
    }

    private static float Note(double seconds, double start, double end, double frequency)
    {
        const double fade = 0.018d;
        var local = seconds - start;
        var envelope = Math.Min(1d, local / fade) * Math.Min(1d, (end - seconds) / fade);
        return (float)(Math.Sin(local * frequency * 2d * Math.PI) * Amplitude * envelope);
    }
}
