using SpeakerKeepAwake.Core;
using SpeakerKeepAwake.Services;

namespace SpeakerKeepAwake.Tests.Services;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"SpeakerKeepAwake-{Guid.NewGuid():N}");

    [Fact]
    public void CorruptFileLoadsSafeDefaults()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "{not json");
        var store = new SettingsStore(path);

        var settings = store.Load();

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public void SaveThenLoadRoundTripsValidatedSettings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new SettingsStore(path);
        var settings = AppSettings.Default with
        {
            SelectedDeviceId = "device-1",
            OnlyWhenDefault = false,
            IdleTimeoutSeconds = 120,
            SignalLevel = SignalLevel.Low
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings, loaded);
        Assert.False(File.Exists(path + ".tmp"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
