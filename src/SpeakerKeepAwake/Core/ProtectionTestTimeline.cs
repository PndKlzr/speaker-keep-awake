namespace SpeakerKeepAwake.Core;

public enum ProtectionTestPhase
{
    KeepAlive,
    Confirmation,
    Complete
}

public readonly record struct ProtectionTestState(ProtectionTestPhase Phase, int SecondsRemaining);

public sealed class ProtectionTestTimeline(DateTimeOffset startedAt)
{
    private static readonly TimeSpan KeepAliveDuration = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan ConfirmationDuration = TimeSpan.FromSeconds(1);

    public ProtectionTestState GetState(DateTimeOffset now)
    {
        var elapsed = now - startedAt;
        if (elapsed >= KeepAliveDuration + ConfirmationDuration)
            return new ProtectionTestState(ProtectionTestPhase.Complete, 0);

        if (elapsed >= KeepAliveDuration)
            return new ProtectionTestState(ProtectionTestPhase.Confirmation, 0);

        var secondsRemaining = Math.Max(1, (int)Math.Ceiling((KeepAliveDuration - elapsed).TotalSeconds));
        return new ProtectionTestState(ProtectionTestPhase.KeepAlive, secondsRemaining);
    }
}
