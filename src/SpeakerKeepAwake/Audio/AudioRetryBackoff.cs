namespace SpeakerKeepAwake.Audio;

public sealed class AudioRetryBackoff(TimeSpan delay)
{
    private DateTimeOffset? _retryAt;

    public bool CanAttempt(DateTimeOffset now) => _retryAt is null || now >= _retryAt;

    public void RecordFailure(DateTimeOffset now) => _retryAt = now + delay;

    public void RecordSuccess() => _retryAt = null;

    public void Reset() => _retryAt = null;
}
