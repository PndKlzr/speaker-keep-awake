using SpeakerKeepAwake.Audio;
using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Tests.Core;

public sealed class KeepAliveEngineTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-09-30T12:00:00Z");

    [Fact]
    public void DefaultOnlyModeDoesNotInspectNonDefaultDevice()
    {
        var catalog = new FakeCatalog { DefaultId = "other" };
        var monitor = new FakeMonitor { Active = true };
        using var renderer = new FakeRenderer();
        using var engine = new KeepAliveEngine(catalog, monitor, renderer);
        engine.UpdateSettings(AppSettings.Default with { SelectedDeviceId = "speaker" });

        engine.Tick(Start);

        Assert.Equal(0, monitor.Calls);
        Assert.False(renderer.IsPlaying);
        Assert.Equal(KeepAliveStatus.Waiting, engine.Status);
    }

    [Fact]
    public void ActiveMediaSessionStartsRendererImmediatelyAndKeepsItRunningThroughSilence()
    {
        var catalog = new FakeCatalog { DefaultId = "speaker" };
        var monitor = new FakeMonitor { Active = true };
        using var renderer = new FakeRenderer();
        using var engine = new KeepAliveEngine(catalog, monitor, renderer);
        engine.UpdateSettings(AppSettings.Default with { SelectedDeviceId = "speaker" });
        engine.Tick(Start);

        Assert.True(renderer.IsPlaying);
        Assert.Equal("speaker", renderer.LastDeviceId);

        monitor.Active = false;
        engine.Tick(Start.AddMilliseconds(100));

        Assert.True(renderer.IsPlaying);
        Assert.Equal(KeepAliveStatus.KeepingAwake, engine.Status);
    }

    [Fact]
    public void FailedRendererUsesRetryBackoff()
    {
        var catalog = new FakeCatalog { DefaultId = "speaker" };
        var monitor = new FakeMonitor { Active = true };
        using var renderer = new FakeRenderer { FailuresRemaining = 1 };
        using var engine = new KeepAliveEngine(catalog, monitor, renderer, TimeSpan.FromSeconds(5));
        engine.UpdateSettings(AppSettings.Default with { SelectedDeviceId = "speaker" });
        engine.Tick(Start);

        engine.Tick(Start.AddSeconds(1));
        engine.Tick(Start.AddSeconds(2));
        engine.Tick(Start.AddSeconds(6));

        Assert.Equal(2, renderer.StartCalls);
        Assert.True(renderer.IsPlaying);
    }

    [Fact]
    public void RepeatedMonitorFailureUsesRetryBackoff()
    {
        var catalog = new FakeCatalog { DefaultId = "speaker" };
        var monitor = new FakeMonitor { Exception = new InvalidOperationException("simulated") };
        using var renderer = new FakeRenderer();
        using var engine = new KeepAliveEngine(catalog, monitor, renderer, TimeSpan.FromSeconds(5));
        engine.UpdateSettings(AppSettings.Default with { SelectedDeviceId = "speaker" });

        engine.Tick(Start);
        engine.Tick(Start.AddSeconds(1));
        engine.Tick(Start.AddSeconds(5));

        Assert.Equal(2, monitor.Calls);
        Assert.False(renderer.IsPlaying);
    }

    private sealed class FakeCatalog : IAudioDeviceCatalog
    {
        public string? DefaultId { get; init; }
        public IReadOnlyList<AudioDeviceInfo> GetActiveDevices() => [new("speaker", "Speaker")];
        public string? GetDefaultDeviceId() => DefaultId;
        public bool IsDevicePresent(string? deviceId) => deviceId == "speaker";
        public void Dispose() { }
    }

    private sealed class FakeMonitor : IAudioActivityMonitor
    {
        public bool Active { get; set; }
        public Exception? Exception { get; init; }
        public int Calls { get; private set; }
        public bool IsRealAudioActive(string deviceId)
        {
            Calls++;
            if (Exception is not null) throw Exception;
            return Active;
        }
    }

    private sealed class FakeRenderer : IKeepAliveRenderer
    {
        public bool IsPlaying { get; private set; }
        public int FailuresRemaining { get; set; }
        public int StartCalls { get; private set; }
        public string? LastDeviceId { get; private set; }

        public void Start(string deviceId, SignalLevel level)
        {
            StartCalls++;
            LastDeviceId = deviceId;
            if (FailuresRemaining-- > 0) throw new InvalidOperationException("simulated");
            IsPlaying = true;
        }

        public void Stop() => IsPlaying = false;
        public void Dispose() { }
    }
}
