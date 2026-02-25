namespace ZSR.Underwriting.Domain.Exceptions;

public class ClaudeRateLimitException : Exception
{
    public int? RetryAfterSeconds { get; }

    public ClaudeRateLimitException(int? retryAfterSeconds)
        : base($"Claude API rate limit exceeded.{(retryAfterSeconds.HasValue ? $" Retry after {retryAfterSeconds} seconds." : "")}")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
