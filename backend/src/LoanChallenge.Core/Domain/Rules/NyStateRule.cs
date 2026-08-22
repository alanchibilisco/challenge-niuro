using LoanChallenge.Core.Domain;

namespace LoanChallenge.Core.Domain.Rules;

/// <summary>Regla de denegación: solicitudes con estado NY.</summary>
public sealed class NyStateRule : ILoanDenialRule
{
    public string Code => "ny_state";

    public string Reason => "No se pueden procesar solicitudes desde el estado de Nueva York (NY).";

    public bool AppliesTo(LoanRequest request) =>
        request.State.Equals("NY", StringComparison.OrdinalIgnoreCase);
}
