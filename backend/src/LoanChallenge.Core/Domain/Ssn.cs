namespace LoanChallenge.Core.Domain;

public static class Ssn
{
    /// <summary>Normaliza un SSN a 9 dígitos (acepta "111-11-1111" o "111111111").</summary>
    public static string Normalize(string ssn) => new(ssn.Where(char.IsAsciiDigit).ToArray());
}
