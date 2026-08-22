namespace LoanChallenge.Core.Application;

public sealed record SubmitResult(
    string Status,
    int? CustomerId,
    int? ApplicationId,
    bool IsNewCustomer,
    string? DenialCode,
    string? DenialReason)
{
    public static SubmitResult Approved(int customerId, int applicationId, bool isNewCustomer) =>
        new("Approved", customerId, applicationId, isNewCustomer, null, null);

    public static SubmitResult Denied(string code, string reason) =>
        new("Denied", null, null, false, code, reason);
}
