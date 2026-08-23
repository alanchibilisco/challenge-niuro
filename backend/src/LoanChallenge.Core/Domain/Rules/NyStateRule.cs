using LoanChallenge.Core.Domain;

namespace LoanChallenge.Core.Domain.Rules;

/// <summary>Regla de denegación: solicitudes con estado NY.</summary>
public sealed class NyStateRule : ILoanDenialRule
{
    public string Code => "ny_state";

    public string Reason => "No se pueden procesar solicitudes desde el estado de Nueva York (NY).";

    private static readonly HashSet<string> NewYorkValues =
    new(StringComparer.OrdinalIgnoreCase)
    {
        "NY",
        "Nueva York",
        "New York"
    };

    public bool AppliesTo(LoanRequest request) =>
      NewYorkValues.Contains(request.State);
}
