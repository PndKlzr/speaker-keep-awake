using SpeakerKeepAwake.Audio;

namespace SpeakerKeepAwake.Core;

public sealed class KeepAliveEngine : IDisposable
{
    private readonly IAudioDeviceCatalog _catalog;
    private readonly IAudioActivityMonitor _monitor;
    private readonly IKeepAliveRenderer _renderer;
    private readonly AudioRetryBackoff _monitorRetryBackoff;
    private readonly AudioRetryBackoff _rendererRetryBackoff;
    private AppSettings _settings = AppSettings.Default;
    private KeepAliveController _controller = new(TimeSpan.FromSeconds(60));
    private string? _effectiveDeviceId;
    private string? _cachedDefaultDeviceId;
    private DateTimeOffset? _nextDefaultLookup;

    public KeepAliveEngine(
        IAudioDeviceCatalog catalog,
        IAudioActivityMonitor monitor,
        IKeepAliveRenderer renderer,
        TimeSpan? retryDelay = null)
    {
        _catalog = catalog;
        _monitor = monitor;
        _renderer = renderer;
        var delay = retryDelay ?? TimeSpan.FromSeconds(5);
        _monitorRetryBackoff = new AudioRetryBackoff(delay);
        _rendererRetryBackoff = new AudioRetryBackoff(delay);
    }

    public KeepAliveStatus Status => _controller.Status;
    public TimeSpan? RemainingKeepAliveTime => _controller.RemainingKeepAliveTime;
    public string? EffectiveDeviceId => _effectiveDeviceId;
    public string? LastError { get; private set; }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings.Validate();
        _renderer.Stop();
        _effectiveDeviceId = null;
        _cachedDefaultDeviceId = null;
        _nextDefaultLookup = null;
        _controller = new KeepAliveController(TimeSpan.FromSeconds(_settings.IdleTimeoutSeconds));
        LastError = null;
    }

    public void Tick(DateTimeOffset now)
    {
        string? deviceId;
        try
        {
            deviceId = _settings.OnlyWhenDefault
                ? GetCurrentDefaultDeviceId(now)
                : _settings.SelectedDeviceId;
        }
        catch (Exception exception)
        {
            SetEffectiveDevice(null);
            LastError = _settings.Enabled ? exception.Message : null;
            _renderer.Stop();
            if (_settings.Enabled) _controller.Update(now, false, false, false);
            else _controller.Disable();
            _monitorRetryBackoff.RecordFailure(now);
            return;
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            SetEffectiveDevice(null);
            Apply(_settings.Enabled
                ? _controller.Update(now, false, false, false)
                : _controller.Disable(), now);
            return;
        }

        try
        {
            var present = _catalog.IsDevicePresent(deviceId);
            if (!present)
            {
                SetEffectiveDevice(null);
                if (_settings.Enabled) _controller.Update(now, false, false, false);
                else _controller.Disable();
                _monitorRetryBackoff.RecordSuccess();
                return;
            }

            SetEffectiveDevice(deviceId);

            if (!_settings.Enabled)
            {
                Apply(_controller.Disable(), now);
                return;
            }

            if (!_monitorRetryBackoff.CanAttempt(now))
            {
                Apply(_controller.Update(now, false, false, false), now);
                return;
            }

            var realAudio = _monitor.IsRealAudioActive(deviceId);
            _monitorRetryBackoff.RecordSuccess();
            Apply(_controller.Update(now, true, true, realAudio), now);

            if ((_controller.Status == KeepAliveStatus.RealAudio ||
                 _controller.Status == KeepAliveStatus.KeepingAwake) &&
                !_renderer.IsPlaying)
            {
                TryStart(now);
            }
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            _renderer.Stop();
            _controller.Update(now, false, false, false);
            _monitorRetryBackoff.RecordFailure(now);
        }
    }

    private string? GetCurrentDefaultDeviceId(DateTimeOffset now)
    {
        if (_nextDefaultLookup is null || now >= _nextDefaultLookup)
        {
            _cachedDefaultDeviceId = _catalog.GetDefaultDeviceId();
            _nextDefaultLookup = now.AddMilliseconds(500);
        }

        return _cachedDefaultDeviceId;
    }

    private void SetEffectiveDevice(string? deviceId)
    {
        if (string.Equals(_effectiveDeviceId, deviceId, StringComparison.Ordinal)) return;

        _renderer.Stop();
        _effectiveDeviceId = deviceId;
        _controller = new KeepAliveController(TimeSpan.FromSeconds(_settings.IdleTimeoutSeconds));
        _monitorRetryBackoff.Reset();
        _rendererRetryBackoff.Reset();
    }

    private void Apply(KeepAliveAction action, DateTimeOffset now)
    {
        if (action == KeepAliveAction.Stop)
        {
            _renderer.Stop();
        }
        else if (action == KeepAliveAction.Start)
        {
            TryStart(now);
        }
    }

    private void TryStart(DateTimeOffset now)
    {
        if (!_rendererRetryBackoff.CanAttempt(now) || string.IsNullOrWhiteSpace(_effectiveDeviceId))
        {
            return;
        }

        try
        {
            _renderer.Start(_effectiveDeviceId, _settings.SignalLevel);
            _rendererRetryBackoff.RecordSuccess();
            LastError = null;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            _renderer.Stop();
            _rendererRetryBackoff.RecordFailure(now);
        }
    }

    public void Dispose()
    {
        _renderer.Dispose();
        if (_monitor is IDisposable disposableMonitor) disposableMonitor.Dispose();
        _catalog.Dispose();
    }
}
