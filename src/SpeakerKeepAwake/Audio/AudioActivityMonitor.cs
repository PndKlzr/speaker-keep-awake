using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace SpeakerKeepAwake.Audio;

public interface IAudioActivityMonitor
{
    bool IsRealAudioActive(string deviceId);
}

public sealed class AudioActivityMonitor : IAudioActivityMonitor, IDisposable
{
    private const float ActivityThreshold = 0.0005f;
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly int _currentProcessId = Environment.ProcessId;

    public bool IsRealAudioActive(string deviceId)
    {
        try
        {
            using var device = _enumerator.GetDevice(deviceId);
            using var manager = device.AudioSessionManager;
            manager.RefreshSessions();
            var sessions = manager.Sessions;
            var samples = new List<AudioSessionSample>(sessions.Count);

            for (var index = 0; index < sessions.Count; index++)
            {
                using var session = sessions[index];
                using var meter = session.AudioMeterInformation;
                var sessionIdentifier = session.GetSessionIdentifier;
                // FxSound mirrors the app's keep-alive tone in its own session; don't treat that as user audio.
                samples.Add(new AudioSessionSample(
                    (int)session.GetProcessID,
                    session.State == AudioSessionState.AudioSessionStateActive,
                    meter.MasterPeakValue,
                    sessionIdentifier?.Contains("\\FxSound.exe", StringComparison.OrdinalIgnoreCase) == true));
            }

            return AudioActivityPolicy.AnyOtherSessionAudible(samples, _currentProcessId, ActivityThreshold);
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException)
        {
            return false;
        }
    }

    public void Dispose() => _enumerator.Dispose();
}
