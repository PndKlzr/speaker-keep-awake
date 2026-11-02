using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.UI;

public enum StatusTone
{
    Neutral,
    Active,
    Waiting,
    Error
}

public sealed record StatusPresentation(string Title, string Detail, StatusTone Tone)
{
    public static StatusPresentation Create(
        KeepAliveStatus status,
        string? error,
        TimeSpan? remaining)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return new("Não foi possível acessar a saída", error, StatusTone.Error);
        }

        return status switch
        {
            KeepAliveStatus.Disabled => new(
                "Proteção desligada",
                "Nenhum sinal está sendo enviado",
                StatusTone.Neutral),
            KeepAliveStatus.Waiting => new(
                "Aguardando a saída de áudio",
                "Conecte o dispositivo ou torne-o a saída padrão",
                StatusTone.Waiting),
            KeepAliveStatus.RealAudio => new(
                "Áudio detectado",
                "A saída está protegida durante a reprodução",
                StatusTone.Active),
            KeepAliveStatus.KeepingAwake => new(
                "Mantendo a caixa acordada",
                FormatCountdown(remaining),
                StatusTone.Active),
            _ => new(
                "Monitorando em segundo plano",
                "A proteção entra em ação quando houver áudio",
                StatusTone.Neutral)
        };
    }

    private static string FormatCountdown(TimeSpan? remaining)
    {
        if (remaining is null) return "Protegendo durante esta pausa";

        var seconds = Math.Max(0, (int)Math.Ceiling(remaining.Value.TotalSeconds));
        if (seconds < 60) return $"O sinal será desligado em {seconds} s";

        var minutes = seconds / 60;
        var leftoverSeconds = seconds % 60;
        return leftoverSeconds == 0
            ? $"O sinal será desligado em {minutes} min"
            : $"O sinal será desligado em {minutes} min {leftoverSeconds} s";
    }
}
