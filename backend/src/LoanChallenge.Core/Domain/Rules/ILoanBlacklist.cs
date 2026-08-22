namespace LoanChallenge.Core.Domain.Rules;

/// <summary>Fuente de la lista negra de números de Seguro Social.</summary>
public interface ILoanBlacklist
{
    /// <param name="normalizedSsn">SSN ya normalizado (9 dígitos).</param>
    bool Contains(string normalizedSsn);
}
