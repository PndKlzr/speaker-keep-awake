using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Tests.Core;

public sealed class KeepAliveControllerTests
{
    private static readonly DateTimeOffset Start = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DoesNotWakeDeviceBeforeRealAudioWasSeen()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));

        var action = controller.Update(Start, eligible: true, devicePresent: true, realAudioActive: false);

        Assert.Equal(KeepAliveAction.None, action);
        Assert.Equal(KeepAliveStatus.Monitoring, controller.Status);
    }

    [Fact]
    public void StartsAsSoonAsMediaSessionBecomesActive()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));

        var action = controller.Update(Start, true, true, true);

        Assert.Equal(KeepAliveAction.Start, action);
        Assert.Equal(KeepAliveStatus.RealAudio, controller.Status);
    }

    [Fact]
    public void StopsAtConfiguredSilenceLimit()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));
        Assert.Equal(KeepAliveAction.Start, controller.Update(Start, true, true, true));
        Assert.Equal(KeepAliveAction.None, controller.Update(Start.AddMilliseconds(100), true, true, false));

        var action = controller.Update(Start.AddMinutes(1), true, true, false);

        Assert.Equal(KeepAliveAction.Stop, action);
        Assert.Equal(KeepAliveStatus.Monitoring, controller.Status);
    }

    [Fact]
    public void ReportsRemainingKeepAliveTimeDuringSilence()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));
        controller.Update(Start, true, true, true);

        controller.Update(Start.AddSeconds(17), true, true, false);

        Assert.Equal(TimeSpan.FromSeconds(43), controller.RemainingKeepAliveTime);
    }

    [Fact]
    public void DoesNotReportCountdownWhileRealAudioIsPlaying()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));

        controller.Update(Start, true, true, true);

        Assert.Null(controller.RemainingKeepAliveTime);
    }

    [Fact]
    public void ReturningMediaSessionKeepsRendererRunningAndResetsTimer()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));
        controller.Update(Start, true, true, true);
        controller.Update(Start.AddSeconds(1), true, true, false);

        var active = controller.Update(Start.AddSeconds(10), true, true, true);
        var silence = controller.Update(Start.AddSeconds(11), true, true, false);

        Assert.Equal(KeepAliveAction.None, active);
        Assert.Equal(KeepAliveAction.None, silence);
        Assert.Equal(KeepAliveStatus.KeepingAwake, controller.Status);
    }

    [Fact]
    public void MissingDeviceStopsAndRequiresNewRealAudioAfterReconnect()
    {
        var controller = new KeepAliveController(TimeSpan.FromMinutes(1));
        controller.Update(Start, true, true, true);
        controller.Update(Start.AddSeconds(1), true, true, false);

        var stop = controller.Update(Start.AddSeconds(2), true, false, false);
        var afterReconnect = controller.Update(Start.AddSeconds(3), true, true, false);

        Assert.Equal(KeepAliveAction.Stop, stop);
        Assert.Equal(KeepAliveAction.None, afterReconnect);
        Assert.Equal(KeepAliveStatus.Monitoring, controller.Status);
    }
}
