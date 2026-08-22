using LoanChallenge.Core.Domain;

namespace LoanChallenge.Core.Domain.Rules;

/// <summary>
/// Regla de denegación del motor. Cada regla es una clase independiente:
/// agregar una regla nueva no requiere tocar las existentes.
/// </summary>
public interface ILoanDenialRule
{
    /// <summary>Código estable del motivo (lo consume el frontend).</summary>
    string Code { get; }

    /// <summary>Motivo legible de la denegación.</summary>
    string Reason { get; }

    bool AppliesTo(LoanRequest request);
}
