using SpeakerKeepAwake.Services;

namespace SpeakerKeepAwake.Tests.Services;

public sealed class StartupRegistrationTests
{
    [Fact]
    public void EnablingWritesQuotedExecutablePath()
    {
        var registry = new FakeStartupRegistry();
        var registration = new StartupRegistration(@"C:\Apps With Spaces\SpeakerKeepAwake.exe", registry);

        registration.SetEnabled(true);

        Assert.Equal("\"C:\\Apps With Spaces\\SpeakerKeepAwake.exe\"", registry.Value);
        Assert.True(registration.IsEnabled);
    }

    [Fact]
    public void EnablingRepairsMovedExecutablePath()
    {
        var registry = new FakeStartupRegistry { Value = "\"C:\\Old\\SpeakerKeepAwake.exe\"" };
        var registration = new StartupRegistration(@"D:\Portable\SpeakerKeepAwake.exe", registry);

        registration.SetEnabled(true);

        Assert.Equal("\"D:\\Portable\\SpeakerKeepAwake.exe\"", registry.Value);
    }

    [Fact]
    public void DisablingDeletesRegistration()
    {
        var registry = new FakeStartupRegistry { Value = "anything" };
        var registration = new StartupRegistration(@"C:\SpeakerKeepAwake.exe", registry);

        registration.SetEnabled(false);

        Assert.Null(registry.Value);
        Assert.False(registration.IsEnabled);
    }

    private sealed class FakeStartupRegistry : IStartupRegistry
    {
        public string? Value { get; set; }
        public string? Read() => Value;
        public void Write(string value) => Value = value;
        public void Delete() => Value = null;
    }
}
