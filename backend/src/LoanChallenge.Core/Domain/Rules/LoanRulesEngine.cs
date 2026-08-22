using LoanChallenge.Core.Domain;

namespace LoanChallenge.Core.Domain.Rules;

/// <summary>
/// Motor de reglas: evalúa las reglas de denegación registradas en orden.
/// Si alguna aplica, la solicitud se deniega con su motivo; si ninguna, se aprueba.
/// </summary>
public sealed class LoanRulesEngine(IEnumerable<ILoanDenialRule> denialRules)
{
    public RuleDecision Decide(LoanRequest request)
    {
        foreach (var rule in denialRules)
        {
            if (rule.AppliesTo(request))
            {
                return RuleDecision.Denied(rule.Code, rule.Reason);
            }
        }

        return RuleDecision.Approved();
    }
}
