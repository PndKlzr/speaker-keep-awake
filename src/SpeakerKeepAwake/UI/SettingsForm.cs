using System.Runtime.InteropServices;
using SpeakerKeepAwake.Audio;
using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.UI;

public sealed class SettingsForm : Form
{
    private const int DwmUseImmersiveDarkMode = 20;
    private const int DwmCaptionColor = 35;
    private const int DwmTextColor = 36;
    private readonly IAudioDeviceCatalog _catalog;
    private readonly ThemePalette _theme = ThemePalette.FromSystem();
    private readonly ComboBox _device = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToggleSwitch _onlyDefault = new() { AccessibleName = "Usar somente quando esta for a saída padrão" };
    private readonly ComboBox _timeout = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ToggleSwitch _startWithWindows = new() { AccessibleName = "Iniciar com o Windows" };
    private readonly ToggleSwitch _protection = new() { AccessibleName = "Ligar ou desligar proteção" };
    private readonly ComboBox _signalLevel = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _protectionState = new();
    private readonly Label _statusDot = new();
    private readonly Label _statusTitle = new();
    private readonly Label _statusDetail = new();
    private readonly Label _savedHint = new();
    private readonly Button _test = new();
    private readonly Button _save = new();
    private readonly Button _advancedToggle = new();
    private readonly RoundedPanel _advancedCard = new();
    private readonly Panel _advancedBody = new();
    private readonly Bitmap _brandImage;
    private AppSettings _settings;
    private bool _allowClose;
    private bool _applyingSettings;
    private bool _advancedVisible;
    private bool _testing;
    private string? _effectiveDeviceId;

    public SettingsForm(IAudioDeviceCatalog catalog, AppSettings settings)
    {
        _catalog = catalog;
        _settings = settings;

        Text = "Speaker Keep Awake";
        ClientSize = new Size(620, 665);
        MinimumSize = new Size(636, 704);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = _theme.WindowBackground;
        ForeColor = _theme.PrimaryText;
        Font = new Font("Segoe UI", 9.5f);
        Icon = AppIcon.Create(32);
        _brandImage = Icon.ToBitmap();

        ConfigureControls();

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(24, 20, 24, 20),
            BackColor = _theme.WindowBackground
        };

        content.Controls.Add(CreateHeader());
        content.Controls.Add(CreateStatusCard());
        content.Controls.Add(CreateOutputCard());
        content.Controls.Add(CreateBehaviorCard());
        content.Controls.Add(CreateAdvancedCard());
        content.Controls.Add(CreateFooter());
        Controls.Add(content);

        _protection.CheckedChanged += (_, _) =>
        {
            if (!_applyingSettings) ToggleProtection(_protection.Checked);
        };
        _onlyDefault.CheckedChanged += (_, _) =>
        {
            if (_applyingSettings) return;
            UpdateOutputModeVisual();
            ReloadDevices(restoreManualSelection: !_onlyDefault.Checked);
        };
        _save.Click += (_, _) => SaveFromUi();
        _test.Click += (_, _) => RequestTest();
        _advancedToggle.Click += (_, _) => SetAdvancedVisible(!_advancedVisible);
        Activated += (_, _) => ReloadDevices();
        Resize += HandleResize;
        FormClosing += HandleFormClosing;

        ApplySettings(settings);
        SetAdvancedVisible(false);
    }

    public event Action<AppSettings>? SettingsSaved;
    public event Action<string, SignalLevel>? TestRequested;
    public event Action? StopTestRequested;

    public void UpdateEffectiveDevice(string? deviceId)
    {
        if (!_onlyDefault.Checked) return;

        var selectedId = (_device.SelectedItem as AudioDeviceInfo)?.Id;
        if (string.Equals(_effectiveDeviceId, deviceId, StringComparison.Ordinal) &&
            string.Equals(selectedId, deviceId, StringComparison.Ordinal))
        {
            return;
        }

        _effectiveDeviceId = deviceId;
        ReloadDevices();
    }

    public void ApplySettings(AppSettings settings)
    {
        _applyingSettings = true;
        try
        {
            _settings = settings;
            _onlyDefault.Checked = settings.OnlyWhenDefault;
            _startWithWindows.Checked = settings.StartWithWindows;
            _protection.Checked = settings.Enabled;
            UpdateProtectionVisual(settings.Enabled);
            UpdateOutputModeVisual();
            _timeout.SelectedItem = _timeout.Items.Cast<TimeoutOption>()
                .First(option => option.Seconds == settings.IdleTimeoutSeconds);
            _signalLevel.SelectedItem = _signalLevel.Items.Cast<SignalOption>()
                .First(option => option.Level == settings.SignalLevel);
            ReloadDevices();
        }
        finally
        {
            _applyingSettings = false;
        }
    }

    public void UpdateStatus(KeepAliveStatus status, string? error, TimeSpan? remaining = null)
    {
        var presentation = StatusPresentation.Create(status, error, remaining);
        _statusTitle.Text = presentation.Title;
        _statusDetail.Text = presentation.Detail;
        _statusDot.ForeColor = presentation.Tone switch
        {
            StatusTone.Active => _theme.IsDark ? Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 65),
            StatusTone.Waiting => _theme.IsDark ? Color.FromArgb(255, 185, 0) : Color.FromArgb(157, 93, 0),
            StatusTone.Error => _theme.IsDark ? Color.FromArgb(255, 110, 100) : Color.FromArgb(196, 43, 28),
            _ => _theme.SecondaryText
        };
    }

    public void ShowSaved()
    {
        _savedHint.Text = "✓ Alterações salvas";
        _savedHint.ForeColor = _theme.IsDark ? Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 65);
    }

    public void SetTesting(bool testing, int secondsRemaining = 0)
    {
        _testing = testing;
        _test.Text = testing
            ? secondsRemaining > 0 ? $"Cancelar teste  ·  {secondsRemaining}s" : "Cancelar teste"
            : "Testar proteção";
        _test.BackColor = testing ? _theme.WindowBackground : _theme.FieldBackground;
    }

    public void UpdateTestStatus(ProtectionTestState state)
    {
        _statusDot.ForeColor = _theme.Accent;
        if (state.Phase == ProtectionTestPhase.Confirmation)
        {
            _statusTitle.Text = "Ouça agora";
            _statusDetail.Text = "Você deve ouvir dois toques completos.";
            return;
        }

        _statusTitle.Text = "Testando proteção";
        _statusDetail.Text = $"Mantendo a caixa ativa · aviso em {state.SecondsRemaining}s";
    }

    public void AllowClose() => _allowClose = true;

    private void ConfigureControls()
    {
        ConfigureCombo(_device);
        ConfigureCombo(_timeout);
        ConfigureCombo(_signalLevel);

        _timeout.Items.AddRange(new object[]
        {
            new TimeoutOption(15, "15 segundos"),
            new TimeoutOption(30, "30 segundos"),
            new TimeoutOption(60, "1 minuto"),
            new TimeoutOption(120, "2 minutos"),
            new TimeoutOption(300, "5 minutos")
        });
        _timeout.DisplayMember = nameof(TimeoutOption.Label);

        _signalLevel.Items.AddRange(new object[]
        {
            new SignalOption(SignalLevel.Minimum, "Muito suave"),
            new SignalOption(SignalLevel.Low, "Suave"),
            new SignalOption(SignalLevel.Medium, "Forte"),
            new SignalOption(SignalLevel.High, "Muito forte")
        });
        _signalLevel.DisplayMember = nameof(SignalOption.Label);

        StyleButton(_save, "Salvar alterações", primary: true);
        StyleButton(_test, "Testar proteção", primary: false);
        StyleButton(_advancedToggle, "Ajuste fino   ▾", primary: false);
        _advancedToggle.Dock = DockStyle.Top;
        _advancedToggle.TextAlign = ContentAlignment.MiddleLeft;
        _advancedToggle.Height = 36;

        _statusDot.Text = "●";
        _statusDot.Font = new Font("Segoe UI", 17f);
        _statusDot.AutoSize = true;
        _statusDot.Anchor = AnchorStyles.Left;

        _statusTitle.AutoSize = true;
        _statusTitle.Font = new Font("Segoe UI Semibold", 12f, FontStyle.Bold);
        _statusTitle.ForeColor = _theme.PrimaryText;
        _statusDetail.AutoSize = true;
        _statusDetail.ForeColor = _theme.SecondaryText;

        _protectionState.AutoSize = true;
        _protectionState.ForeColor = _theme.SecondaryText;
        _protectionState.Anchor = AnchorStyles.Right;

        _savedHint.AutoSize = true;
        _savedHint.Anchor = AnchorStyles.Left;
        _savedHint.ForeColor = _theme.SecondaryText;

        foreach (var toggle in new[] { _protection, _onlyDefault, _startWithWindows })
        {
            toggle.CheckedColor = _theme.Accent;
            toggle.UncheckedColor = _theme.ToggleOff;
            toggle.BackColor = _theme.CardBackground;
        }

        _advancedBody.BackColor = _theme.CardBackground;
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Width = 552,
            Height = 62,
            Margin = new Padding(0, 0, 0, 10),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _theme.WindowBackground
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var icon = new PictureBox
        {
            Image = _brandImage,
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(42, 42),
            Anchor = AnchorStyles.Left
        };
        header.Controls.Add(icon, 0, 0);

        var text = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        text.Controls.Add(new Label
        {
            Text = "Speaker Keep Awake",
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Font = new Font("Segoe UI Semibold", 18f, FontStyle.Bold),
            ForeColor = _theme.PrimaryText
        }, 0, 0);
        text.Controls.Add(new Label
        {
            Text = "Evita cortes de áudio em caixas Bluetooth",
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            ForeColor = _theme.SecondaryText
        }, 0, 1);
        header.Controls.Add(text, 1, 0);
        return header;
    }

    private Control CreateStatusCard()
    {
        var card = CreateCard(100);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_statusDot, 0, 0);

        var text = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        text.Controls.Add(_statusTitle, 0, 0);
        text.Controls.Add(_statusDetail, 0, 1);
        layout.Controls.Add(text, 1, 0);

        var protection = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 15, 0, 0)
        };
        protection.Controls.Add(_protection);
        protection.Controls.Add(_protectionState);
        layout.Controls.Add(protection, 2, 0);
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateOutputCard()
    {
        var card = CreateCard(148);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionTitle("Saída de áudio"), 0, 0);

        var deviceRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        deviceRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _device.Dock = DockStyle.Fill;
        deviceRow.Controls.Add(_device, 0, 0);
        var refresh = new Button();
        StyleButton(refresh, "Atualizar", primary: false);
        refresh.Dock = DockStyle.Fill;
        refresh.Margin = new Padding(8, 0, 0, 4);
        refresh.Click += (_, _) => ReloadDevices();
        deviceRow.Controls.Add(refresh, 1, 0);
        layout.Controls.Add(deviceRow, 0, 1);
        layout.Controls.Add(CreateSwitchRow(
            "Usar somente quando for a saída padrão",
            "Recomendado para não enviar sinal ao dispositivo errado",
            _onlyDefault), 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateBehaviorCard()
    {
        var card = CreateCard(154);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSectionTitle("Comportamento"), 0, 0);

        var timeoutRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        timeoutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        timeoutRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        timeoutRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        timeoutRow.Controls.Add(CreateSettingText("Depois que o áudio parar", "Tempo até desligar o sinal"), 0, 0);
        _timeout.Dock = DockStyle.Fill;
        _timeout.Margin = new Padding(0, 7, 0, 7);
        timeoutRow.Controls.Add(_timeout, 1, 0);
        layout.Controls.Add(timeoutRow, 0, 1);
        layout.Controls.Add(CreateSwitchRow(
            "Iniciar com o Windows",
            "Mantém a proteção disponível sem precisar abrir o app",
            _startWithWindows), 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateAdvancedCard()
    {
        _advancedCard.Width = 552;
        _advancedCard.Height = 52;
        _advancedCard.Margin = new Padding(0, 0, 0, 10);
        _advancedCard.Padding = new Padding(16, 8, 16, 12);
        _advancedCard.BackColor = _theme.CardBackground;
        _advancedCard.BorderColor = _theme.Border;
        _advancedCard.Controls.Add(_advancedToggle);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(CreateSettingText(
            "Força do sinal",
            "Use o menor nível que mantenha a caixa acordada"), 0, 0);
        _signalLevel.Dock = DockStyle.Top;
        _signalLevel.Margin = new Padding(0, 6, 0, 0);
        layout.Controls.Add(_signalLevel, 1, 0);
        var testExplanation = new Label
        {
            Text = "Mantém a saída ativa por 8 segundos e toca dois avisos. Se saírem inteiros, funcionou.",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = _theme.SecondaryText,
            Font = new Font("Segoe UI", 8.5f)
        };
        layout.Controls.Add(testExplanation, 0, 1);
        layout.SetColumnSpan(testExplanation, 2);
        _test.Dock = DockStyle.Right;
        _test.Width = 150;
        layout.Controls.Add(_test, 1, 2);

        _advancedBody.Dock = DockStyle.Bottom;
        _advancedBody.Height = 130;
        _advancedBody.Padding = new Padding(0, 8, 0, 0);
        _advancedBody.Controls.Add(layout);
        _advancedCard.Controls.Add(_advancedBody);
        return _advancedCard;
    }

    private Control CreateFooter()
    {
        var footer = new TableLayoutPanel
        {
            Width = 552,
            Height = 44,
            Margin = new Padding(0),
            ColumnCount = 2,
            RowCount = 1,
            BackColor = _theme.WindowBackground
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154));
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.Controls.Add(_savedHint, 0, 0);
        _save.Dock = DockStyle.Fill;
        footer.Controls.Add(_save, 1, 0);
        return footer;
    }

    private RoundedPanel CreateCard(int height) => new()
    {
        Width = 552,
        Height = height,
        Margin = new Padding(0, 0, 0, 10),
        Padding = new Padding(18, 14, 18, 14),
        BackColor = _theme.CardBackground,
        BorderColor = _theme.Border
    };

    private Label CreateSectionTitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11f, FontStyle.Bold),
        ForeColor = _theme.PrimaryText,
        Anchor = AnchorStyles.Left
    };

    private Control CreateSettingText(string title, string detail)
    {
        var text = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        text.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            ForeColor = _theme.PrimaryText
        }, 0, 0);
        text.Controls.Add(new Label
        {
            Text = detail,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            ForeColor = _theme.SecondaryText,
            Font = new Font("Segoe UI", 8.5f)
        }, 0, 1);
        return text;
    }

    private Control CreateSwitchRow(string title, string detail, ToggleSwitch toggle)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        row.Controls.Add(CreateSettingText(title, detail), 0, 0);
        toggle.Anchor = AnchorStyles.Right;
        row.Controls.Add(toggle, 1, 0);
        return row;
    }

    private static void ConfigureCombo(ComboBox combo)
    {
        combo.Font = new Font("Segoe UI", 9.5f);
        combo.FlatStyle = FlatStyle.System;
        combo.IntegralHeight = false;
        combo.Height = 32;
    }

    private void StyleButton(Button button, string text, bool primary)
    {
        button.Text = text;
        button.Height = 36;
        button.AutoSize = false;
        button.Cursor = Cursors.Hand;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = _theme.Border;
        button.BackColor = primary ? _theme.Accent : _theme.FieldBackground;
        button.ForeColor = primary
            ? (_theme.IsDark ? Color.FromArgb(20, 20, 20) : Color.White)
            : _theme.PrimaryText;
        button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
    }

    private void ReloadDevices(bool restoreManualSelection = false)
    {
        var selectedId = _onlyDefault.Checked
            ? _effectiveDeviceId
            : restoreManualSelection
                ? _settings.SelectedDeviceId
                : (_device.SelectedItem as AudioDeviceInfo)?.Id ?? _settings.SelectedDeviceId;
        try
        {
            var devices = _catalog.GetActiveDevices();
            _device.DataSource = devices.ToList();
            _device.DisplayMember = nameof(AudioDeviceInfo.Name);
            _device.SelectedItem = devices.FirstOrDefault(item => item.Id == selectedId);
        }
        catch (Exception exception)
        {
            UpdateStatus(KeepAliveStatus.Waiting, $"Não foi possível listar as saídas: {exception.Message}");
        }
    }

    private void SaveFromUi()
    {
        var selected = _device.SelectedItem as AudioDeviceInfo;
        if (selected is null && !_onlyDefault.Checked)
        {
            MessageBox.Show(this, "Escolha uma saída de áudio.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _settings = _settings with
        {
            SelectedDeviceId = _onlyDefault.Checked
                ? _settings.SelectedDeviceId
                : selected?.Id ?? _settings.SelectedDeviceId,
            OnlyWhenDefault = _onlyDefault.Checked,
            IdleTimeoutSeconds = ((TimeoutOption)_timeout.SelectedItem!).Seconds,
            StartWithWindows = _startWithWindows.Checked,
            Enabled = _settings.Enabled,
            SignalLevel = ((SignalOption)_signalLevel.SelectedItem!).Level
        };
        SettingsSaved?.Invoke(_settings);
    }

    private void RequestTest()
    {
        if (_testing)
        {
            StopTestRequested?.Invoke();
            return;
        }

        if (_device.SelectedItem is not AudioDeviceInfo selected)
        {
            MessageBox.Show(this, "Escolha uma saída de áudio para testar.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var level = ((SignalOption)_signalLevel.SelectedItem!).Level;
        _settings = _settings with
        {
            SelectedDeviceId = _onlyDefault.Checked ? _settings.SelectedDeviceId : selected.Id,
            SignalLevel = level
        };
        SettingsSaved?.Invoke(_settings);
        TestRequested?.Invoke(selected.Id, level);
    }

    private void SetAdvancedVisible(bool visible)
    {
        _advancedVisible = visible;
        _advancedBody.Visible = visible;
        _advancedCard.Height = visible ? 190 : 52;
        _advancedToggle.Text = visible ? "Ajuste fino   ▴" : "Ajuste fino   ▾";
        var desiredHeight = visible ? 806 : 665;
        var availableHeight = Screen.FromControl(this).WorkingArea.Height - 60;
        ClientSize = new Size(ClientSize.Width, Math.Min(desiredHeight, availableHeight));
    }

    private void ToggleProtection(bool enabled)
    {
        _settings = _settings with { Enabled = enabled };
        UpdateProtectionVisual(enabled);
        SettingsSaved?.Invoke(_settings);
    }

    private void UpdateProtectionVisual(bool enabled)
    {
        _protectionState.Text = enabled ? "Ligada" : "Desligada";
        _protectionState.ForeColor = enabled
            ? (_theme.IsDark ? Color.FromArgb(108, 203, 95) : Color.FromArgb(16, 124, 65))
            : _theme.SecondaryText;
    }

    private void UpdateOutputModeVisual()
    {
        _device.Enabled = !_onlyDefault.Checked;
    }

    private void HandleResize(object? sender, EventArgs eventArgs)
    {
        if (WindowState != FormWindowState.Minimized) return;
        Hide();
        WindowState = FormWindowState.Normal;
    }

    private void HandleFormClosing(object? sender, FormClosingEventArgs eventArgs)
    {
        if (!_allowClose && eventArgs.CloseReason == CloseReason.UserClosing)
        {
            eventArgs.Cancel = true;
            Hide();
        }
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);
        if (!_theme.IsDark) return;

        var enabled = 1;
        DwmSetWindowAttribute(Handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
        var caption = ColorTranslator.ToWin32(_theme.WindowBackground);
        var text = ColorTranslator.ToWin32(_theme.PrimaryText);
        DwmSetWindowAttribute(Handle, DwmCaptionColor, ref caption, sizeof(int));
        DwmSetWindowAttribute(Handle, DwmTextColor, ref text, sizeof(int));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _brandImage.Dispose();
        base.Dispose(disposing);
    }

    private sealed record TimeoutOption(int Seconds, string Label);
    private sealed record SignalOption(SignalLevel Level, string Label);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);
}
