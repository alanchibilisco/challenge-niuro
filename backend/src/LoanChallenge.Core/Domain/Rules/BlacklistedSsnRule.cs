using LoanChallenge.Core.Domain;

namespace LoanChallenge.Core.Domain.Rules;

/// <summary>Regla de denegación: SSN presente en la lista negra.</summary>
public sealed class BlacklistedSsnRule(ILoanBlacklist blacklist) : ILoanDenialRule
{
    public string Code => "ssn_blacklisted";

    public string Reason => "El número de Seguro Social está en la lista negra.";

    public bool AppliesTo(LoanRequest request) => blacklist.Contains(Ssn.Normalize(request.Ssn));
}
