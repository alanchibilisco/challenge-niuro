namespace LoanChallenge.Api.Data;

public static class OutboxStatuses
{
    public const string Pending = "Pending";
    public const string Processed = "Processed";
    public const string Failed = "Failed";
}

public class OutboxMessage
{
    public int Id { get; set; }

    public string Ssn { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public string Status { get; set; } = OutboxStatuses.Pending;

    public int Attempts { get; set; }

    public DateTime CreatedAt { get; set; }=DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }=null;

    public string? LastError { get; set; }=null;
}
