namespace SpeakerKeepAwake.Audio;

public readonly record struct AudioSessionSample(
    int ProcessId,
    bool IsActive,
    float Peak,
    bool IsFxSoundProcessor = false);

public static class AudioActivityPolicy
{
    public static bool AnyOtherSessionAudible(
        IEnumerable<AudioSessionSample> sessions,
        int currentProcessId,
        float threshold) =>
        sessions.Any(session =>
            session.ProcessId != currentProcessId &&
            !session.IsFxSoundProcessor &&
            session.IsActive &&
            session.Peak >= threshold);
}
