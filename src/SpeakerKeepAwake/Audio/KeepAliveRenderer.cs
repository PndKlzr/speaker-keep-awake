using NAudio.CoreAudioApi;
using NAudio.Wave;
using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Audio;

public interface IKeepAliveRenderer : IDisposable
{
    bool IsPlaying { get; }
    void Start(string deviceId, SignalLevel level);
    void Stop();
}

public sealed class KeepAliveRenderer : IKeepAliveRenderer
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private MMDevice? _device;
    private WasapiPlayer? _output;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    public void Start(string deviceId, SignalLevel level)
    {
        Start(deviceId, (sampleRate, channels) => new KeepAliveSampleProvider(
            sampleRate,
            channels,
            SignalLevels.GetAmplitude(level)));
    }

    public void PlayConfirmation(string deviceId)
    {
        Start(deviceId, (sampleRate, channels) => new ConfirmationChimeSampleProvider(sampleRate, channels));
    }

    private void Start(string deviceId, Func<int, int, ISampleProvider> createSource)
    {
        Stop();

        try
        {
            _device = _enumerator.GetDevice(deviceId);
            using var audioClient = _device.CreateAudioClient();
            var mixFormat = audioClient.MixFormat;
            var source = createSource(mixFormat.SampleRate, mixFormat.Channels);

            _output = new WasapiPlayerBuilder()
                .WithDevice(_device)
                .WithSharedMode()
                .WithEventSync()
                .WithLatency(100)
                .Build();
            _output.Init(source.ToWaveProvider());
            _output.Play();
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public void Stop()
    {
        try
        {
            _output?.Stop();
        }
        catch
        {
            // A device can disappear while WASAPI is stopping; disposal below is sufficient.
        }

        _output?.Dispose();
        _output = null;
        _device?.Dispose();
        _device = null;
    }

    public void Dispose()
    {
        Stop();
        _enumerator.Dispose();
    }
}
