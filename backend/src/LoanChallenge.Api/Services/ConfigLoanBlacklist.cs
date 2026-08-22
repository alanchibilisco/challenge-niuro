using LoanChallenge.Api.Options;
using LoanChallenge.Core.Domain;
using LoanChallenge.Core.Domain.Rules;
using Microsoft.Extensions.Options;

namespace LoanChallenge.Api.Services;

/// <summary>Lista negra de SSN leída de la configuración (se normalizan al cargar).</summary>
public sealed class ConfigLoanBlacklist(IOptions<BlacklistOptions> options) : ILoanBlacklist
{
    private readonly HashSet<string> _ssns = options.Value.Ssns
        .Select(Ssn.Normalize)
        .ToHashSet(StringComparer.Ordinal);

    public bool Contains(string normalizedSsn) => _ssns.Contains(normalizedSsn);
}
