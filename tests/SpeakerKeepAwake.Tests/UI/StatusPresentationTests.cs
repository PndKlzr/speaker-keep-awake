using SpeakerKeepAwake.Core;
using SpeakerKeepAwake.UI;

namespace SpeakerKeepAwake.Tests.UI;

public sealed class StatusPresentationTests
{
    [Fact]
    public void KeepingAwakeShowsRoundedUpCountdown()
    {
        var presentation = StatusPresentation.Create(
            KeepAliveStatus.KeepingAwake,
            error: null,
            remaining: TimeSpan.FromSeconds(42.1));

        Assert.Equal("Mantendo a caixa acordada", presentation.Title);
        Assert.Equal("O sinal será desligado em 43 s", presentation.Detail);
        Assert.Equal(StatusTone.Active, presentation.Tone);
    }

    [Fact]
    public void ErrorTakesPriorityOverRegularStatus()
    {
        var presentation = StatusPresentation.Create(
            KeepAliveStatus.RealAudio,
            error: "dispositivo indisponível",
            remaining: null);

        Assert.Equal("Não foi possível acessar a saída", presentation.Title);
        Assert.Equal("dispositivo indisponível", presentation.Detail);
        Assert.Equal(StatusTone.Error, presentation.Tone);
    }
}
