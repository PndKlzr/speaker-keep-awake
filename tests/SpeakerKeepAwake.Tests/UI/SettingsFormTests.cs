using SpeakerKeepAwake.Audio;
using SpeakerKeepAwake.Core;
using SpeakerKeepAwake.UI;

namespace SpeakerKeepAwake.Tests.UI;

public sealed class SettingsFormTests
{
    [Fact]
    public void ConstructsBeforeWindowHandleExists()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var catalog = new FakeCatalog();
                using var form = new SettingsForm(catalog, AppSettings.Default with { SelectedDeviceId = "speaker" });
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }

    private sealed class FakeCatalog : IAudioDeviceCatalog
    {
        public IReadOnlyList<AudioDeviceInfo> GetActiveDevices() => [new("speaker", "Speaker")];
        public string? GetDefaultDeviceId() => "speaker";
        public bool IsDevicePresent(string? deviceId) => deviceId == "speaker";
        public void Dispose() { }
    }
}
