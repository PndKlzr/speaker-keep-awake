using NAudio.CoreAudioApi;

namespace SpeakerKeepAwake.Audio;

public sealed record AudioDeviceInfo(string Id, string Name)
{
    public override string ToString() => Name;
}

public interface IAudioDeviceCatalog : IDisposable
{
    IReadOnlyList<AudioDeviceInfo> GetActiveDevices();
    string? GetDefaultDeviceId();
    bool IsDevicePresent(string? deviceId);
}

public sealed class AudioDeviceCatalog : IAudioDeviceCatalog
{
    private readonly MMDeviceEnumerator _enumerator = new();

    public IReadOnlyList<AudioDeviceInfo> GetActiveDevices()
    {
        var result = new List<AudioDeviceInfo>();
        using var devices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        for (var index = 0; index < devices.Count; index++)
        {
            using var device = devices[index];
            result.Add(new AudioDeviceInfo(device.ID, device.FriendlyName));
        }

        return result.OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public string? GetDefaultDeviceId()
    {
        if (!_enumerator.TryGetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out var device))
        {
            return null;
        }

        using (device)
        {
            return device.ID;
        }
    }

    public bool IsDevicePresent(string? deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return false;

        try
        {
            using var device = _enumerator.GetDevice(deviceId);
            return device.State == DeviceState.Active;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _enumerator.Dispose();
}
