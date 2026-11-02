using SpeakerKeepAwake.Core;

namespace SpeakerKeepAwake.Tests.Core;

public sealed class AppSettingsTests
{
    [Fact]
    public void InvalidValuesReturnSafeDefaults()
    {
        var settings = new AppSettings
        {
            IdleTimeoutSeconds = 17,
            SignalLevel = (SignalLevel)999
        };

        var validated = settings.Validate();

        Assert.Equal(60, validated.IdleTimeoutSeconds);
        Assert.Equal(SignalLevel.Minimum, validated.SignalLevel);
        Assert.True(validated.OnlyWhenDefault);
    }
}
