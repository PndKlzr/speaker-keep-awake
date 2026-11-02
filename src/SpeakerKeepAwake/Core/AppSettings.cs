namespace SpeakerKeepAwake.Core;

public enum SignalLevel
{
    Minimum,
    Low,
    Medium,
    High
}

public sealed record AppSettings
{
    private static readonly int[] AllowedTimeouts = [15, 30, 60, 120, 300];

    public static AppSettings Default { get; } = new();

    public string? SelectedDeviceId { get; init; }
    public bool OnlyWhenDefault { get; init; } = true;
    public int IdleTimeoutSeconds { get; init; } = 60;
    public bool StartWithWindows { get; init; } = true;
    public bool Enabled { get; init; } = true;
    public SignalLevel SignalLevel { get; init; } = SignalLevel.Minimum;

    public AppSettings Validate() => this with
    {
        IdleTimeoutSeconds = AllowedTimeouts.Contains(IdleTimeoutSeconds) ? IdleTimeoutSeconds : 60,
        SignalLevel = Enum.IsDefined(SignalLevel) ? SignalLevel : SignalLevel.Minimum
    };
}
