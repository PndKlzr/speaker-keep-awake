using Microsoft.Win32;

namespace SpeakerKeepAwake.Services;

public interface IStartupRegistry
{
    string? Read();
    void Write(string value);
    void Delete();
}

public sealed class WindowsStartupRegistry : IStartupRegistry
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SpeakerKeepAwake";

    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
        return key?.GetValue(ValueName) as string;
    }

    public void Write(string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        key.SetValue(ValueName, value, RegistryValueKind.String);
    }

    public void Delete()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}

public sealed class StartupRegistration(string executablePath, IStartupRegistry registry)
{
    private string Command => $"\"{executablePath}\"";

    public bool IsEnabled => string.Equals(registry.Read(), Command, StringComparison.OrdinalIgnoreCase);

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            registry.Write(Command);
        }
        else
        {
            registry.Delete();
        }
    }
}
