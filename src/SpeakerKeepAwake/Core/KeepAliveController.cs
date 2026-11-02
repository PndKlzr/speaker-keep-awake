namespace SpeakerKeepAwake.Core;

public enum KeepAliveAction
{
    None,
    Start,
    Stop
}

public enum KeepAliveStatus
{
    Disabled,
    Waiting,
    Monitoring,
    RealAudio,
    KeepingAwake
}

public sealed class KeepAliveController(TimeSpan idleTimeout)
{
    private DateTimeOffset? _lastRealAudio;
    private bool _hasSeenRealAudio;
    private bool _isKeepingAwake;

    public KeepAliveStatus Status { get; private set; } = KeepAliveStatus.Monitoring;
    public TimeSpan? RemainingKeepAliveTime { get; private set; }

    public KeepAliveAction Update(
        DateTimeOffset now,
        bool eligible,
        bool devicePresent,
        bool realAudioActive)
    {
        if (!eligible || !devicePresent)
        {
            _hasSeenRealAudio = false;
            _lastRealAudio = null;
            Status = KeepAliveStatus.Waiting;
            RemainingKeepAliveTime = null;
            return SetKeepingAwake(false);
        }

        if (realAudioActive)
        {
            _hasSeenRealAudio = true;
            _lastRealAudio = now;
            Status = KeepAliveStatus.RealAudio;
            RemainingKeepAliveTime = null;
            return SetKeepingAwake(true);
        }

        if (_hasSeenRealAudio && _lastRealAudio is not null && now - _lastRealAudio < idleTimeout)
        {
            Status = KeepAliveStatus.KeepingAwake;
            RemainingKeepAliveTime = idleTimeout - (now - _lastRealAudio.Value);
            return SetKeepingAwake(true);
        }

        _hasSeenRealAudio = false;
        _lastRealAudio = null;
        Status = KeepAliveStatus.Monitoring;
        RemainingKeepAliveTime = null;
        return SetKeepingAwake(false);
    }

    public KeepAliveAction Disable()
    {
        _hasSeenRealAudio = false;
        _lastRealAudio = null;
        Status = KeepAliveStatus.Disabled;
        RemainingKeepAliveTime = null;
        return SetKeepingAwake(false);
    }

    private KeepAliveAction SetKeepingAwake(bool shouldKeepAwake)
    {
        if (_isKeepingAwake == shouldKeepAwake)
        {
            return KeepAliveAction.None;
        }

        _isKeepingAwake = shouldKeepAwake;
        return shouldKeepAwake ? KeepAliveAction.Start : KeepAliveAction.Stop;
    }
}
