namespace LoanChallenge.Api.Options;

public sealed class OutboxOptions
{
    public bool Enabled { get; init; } = true;

    public int PollIntervalSeconds { get; init; } = 5;

    public int MaxAttempts { get; init; } = 5;
}
