using System.Drawing;
using SpeakerKeepAwake.Audio;
using SpeakerKeepAwake.Core;
using SpeakerKeepAwake.Services;
using SpeakerKeepAwake.UI;

namespace SpeakerKeepAwake;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsStore _settingsStore = new(SettingsStore.DefaultPath);
    private readonly AudioDeviceCatalog _catalog = new();
    private readonly KeepAliveEngine _engine;
    private readonly StartupRegistration _startup;
    private readonly Icon _appIcon = AppIcon.Create(16);
    private readonly NotifyIcon _trayIcon;
    private readonly TrayContextMenu _trayMenu;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 200 };
    private SettingsForm _form;
    private AppSettings _settings;
    private KeepAliveRenderer? _testRenderer;
    private ProtectionTestTimeline? _testTimeline;
    private string? _testDeviceId;
    private bool _testConfirmationStarted;
    private DateTimeOffset _nextOutputUiSync;

    public TrayApplicationContext()
    {
        _settings = _settingsStore.Load();
        _engine = new KeepAliveEngine(_catalog, new AudioActivityMonitor(), new KeepAliveRenderer());
        _engine.UpdateSettings(_settings);
        _startup = new StartupRegistration(Application.ExecutablePath, new WindowsStartupRegistry());
        _form = CreateForm();
        TryUpdateStartup(_settings.StartWithWindows);

        _trayMenu = new TrayContextMenu(ThemePalette.FromSystem(), _settings.Enabled);
        _trayMenu.OpenItem.Click += (_, _) => ShowSettings();
        _trayMenu.ProtectionItem.Click += (_, _) => ToggleEnabled(!_settings.Enabled);
        _trayMenu.ExitItem.Click += (_, _) => Exit();

        _trayIcon = new NotifyIcon
        {
            Visible = true,
            Icon = _appIcon,
            Text = "Speaker Keep Awake — iniciando",
            ContextMenuStrip = _trayMenu
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        if (string.IsNullOrWhiteSpace(_settings.SelectedDeviceId)) ShowSettings();
    }

    private SettingsForm CreateForm()
    {
        var form = new SettingsForm(_catalog, _settings);
        form.SettingsSaved += SaveSettings;
        form.TestRequested += StartTest;
        form.StopTestRequested += StopTest;
        return form;
    }

    private void Tick()
    {
        var now = DateTimeOffset.Now;
        if (_testTimeline is not null)
        {
            var state = _testTimeline.GetState(now);
            if (state.Phase == ProtectionTestPhase.Complete)
            {
                StopTest();
            }
            else
            {
                if (state.Phase == ProtectionTestPhase.Confirmation && !_testConfirmationStarted)
                {
                    try
                    {
                        _testRenderer!.PlayConfirmation(_testDeviceId!);
                        _testConfirmationStarted = true;
                    }
                    catch (Exception exception)
                    {
                        StopTest();
                        _form.UpdateStatus(KeepAliveStatus.Waiting, $"teste de áudio: {exception.Message}");
                        return;
                    }
                }

                _form.SetTesting(true, state.SecondsRemaining);
                _form.UpdateTestStatus(state);
                return;
            }
        }

        _engine.Tick(now);
        if (_settings.OnlyWhenDefault && _form.Visible && now >= _nextOutputUiSync)
        {
            _form.UpdateEffectiveDevice(_engine.EffectiveDeviceId);
            _nextOutputUiSync = now.AddSeconds(1);
        }
        _form.UpdateStatus(_engine.Status, _engine.LastError, _engine.RemainingKeepAliveTime);
        UpdateTray(_engine.Status, _engine.LastError);
    }

    private void SaveSettings(AppSettings settings)
    {
        StopTest();
        try
        {
            _settingsStore.Save(settings);
            TryUpdateStartup(settings.StartWithWindows);
            _settings = settings;
            _engine.UpdateSettings(settings);
            _trayMenu.SetProtectionEnabled(settings.Enabled);
            _form.UpdateStatus(_engine.Status, null);
            _form.ShowSaved();
        }
        catch (Exception exception)
        {
            MessageBox.Show(_form, $"Não foi possível salvar as configurações:\n{exception.Message}", "Speaker Keep Awake", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleEnabled(bool enabled)
    {
        if (_settings.Enabled == enabled) return;
        SaveSettings(_settings with { Enabled = enabled });
        _form.ApplySettings(_settings);
    }

    private void StartTest(string deviceId, SignalLevel level)
    {
        StopTest();
        try
        {
            _engine.UpdateSettings(_settings);
            _testRenderer = new KeepAliveRenderer();
            _testRenderer.Start(deviceId, level);
            _testTimeline = new ProtectionTestTimeline(DateTimeOffset.Now);
            _testDeviceId = deviceId;
            _testConfirmationStarted = false;
            _form.SetTesting(true, 8);
            _form.UpdateTestStatus(_testTimeline.GetState(DateTimeOffset.Now));
        }
        catch (Exception exception)
        {
            StopTest();
            MessageBox.Show(_form, $"Não foi possível iniciar o teste:\n{exception.Message}", "Speaker Keep Awake", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopTest()
    {
        _testRenderer?.Dispose();
        _testRenderer = null;
        _testTimeline = null;
        _testDeviceId = null;
        _testConfirmationStarted = false;
        _form.SetTesting(false);
    }

    private void TryUpdateStartup(bool enabled)
    {
        try
        {
            _startup.SetEnabled(enabled);
        }
        catch (Exception exception)
        {
            _form.UpdateStatus(KeepAliveStatus.Waiting, $"inicialização automática: {exception.Message}");
        }
    }

    private void ShowSettings()
    {
        if (_form.IsDisposed) _form = CreateForm();
        _form.UpdateEffectiveDevice(_engine.EffectiveDeviceId);
        _form.WindowState = FormWindowState.Normal;
        _form.Show();
        _form.Activate();
        _form.BringToFront();
    }

    private void UpdateTray(KeepAliveStatus status, string? error)
    {
        _trayIcon.Text = status == KeepAliveStatus.KeepingAwake
            ? "Speaker Keep Awake — mantendo ativo"
            : status == KeepAliveStatus.RealAudio
                ? "Speaker Keep Awake — áudio detectado"
                : "Speaker Keep Awake — monitorando";
    }

    private void Exit()
    {
        _timer.Stop();
        StopTest();
        _engine.Dispose();
        _form.AllowClose();
        _form.Close();
        _form.Dispose();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
        _appIcon.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}
