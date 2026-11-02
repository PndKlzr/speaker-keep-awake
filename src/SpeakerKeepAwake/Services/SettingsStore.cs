using System.Text.Json;
using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Services;

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}

public sealed class SettingsStore(string path) : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SpeakerKeepAwake",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return AppSettings.Default;
            }

            return (JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions)
                    ?? AppSettings.Default)
                .Validate();
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
        catch (IOException)
        {
            return AppSettings.Default;
        }
    }

    public void Save(AppSettings settings)
    {
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("O caminho de configurações não tem uma pasta.");
        Directory.CreateDirectory(directory);

        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings.Validate(), JsonOptions));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
