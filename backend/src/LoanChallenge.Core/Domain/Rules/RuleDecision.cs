namespace LoanChallenge.Core.Domain.Rules;

public sealed record RuleDecision(bool IsApproved, string? DenialCode, string? DenialReason)
{
    public static RuleDecision Approved() => new(true, null, null);

    public static RuleDecision Denied(string code, string reason) => new(false, code, reason);
}
