using LoanChallenge.Core.Domain;
using LoanChallenge.Core.Domain.Rules;

namespace LoanChallenge.Tests;

public class RulesEngineTests
{
    private static LoanRulesEngine CreateEngine(string[] blacklistedSsns) =>
        new([new NyStateRule(), new BlacklistedSsnRule(new FakeBlacklist(blacklistedSsns))]);

    [Fact]
    public void Estado_NY_deniega_con_motivo_ny_state()
    {
        var engine = CreateEngine([]);
        var request = ValidRequest() with { State = "NY" };

        var decision = engine.Decide(request);

        Assert.False(decision.IsApproved);
        Assert.Equal("ny_state", decision.DenialCode);
    }

    [Fact]
    public void Estado_NY_es_caso_insensible()
    {
        var engine = CreateEngine([]);
        var request = ValidRequest() with { State = "ny" };

        var decision = engine.Decide(request);

        Assert.False(decision.IsApproved);
    }

    [Fact]
    public void Ssn_en_lista_negra_deniega_con_motivo_ssn_blacklisted()
    {
        var engine = CreateEngine(["111-11-1111"]);
        var request = ValidRequest() with { Ssn = "111-11-1111" };

        var decision = engine.Decide(request);

        Assert.False(decision.IsApproved);
        Assert.Equal("ssn_blacklisted", decision.DenialCode);
    }

    [Fact]
    public void Ssn_blacklisted_con_formato_sin_guiones_tambien_deniega()
    {
        var engine = CreateEngine(["111111111"]);
        var request = ValidRequest() with { Ssn = "111-11-1111" };

        var decision = engine.Decide(request);

        Assert.False(decision.IsApproved);
    }

    [Fact]
    public void Solicitud_valida_se_aprueba()
    {
        var engine = CreateEngine(["999999999"]);
        var request = ValidRequest() with { Ssn = "123-45-6789", State = "CA" };

        var decision = engine.Decide(request);

        Assert.True(decision.IsApproved);
        Assert.Null(decision.DenialCode);
    }

    private static LoanRequest ValidRequest() => new(
        FirstName: "Ana",
        LastName: "Gomez",
        Address: "123 Main St",
        State: "CA",
        CompanyName: "Acme Inc",
        RequestedAmount: 10_000m,
        Ssn: "123-45-6789");

    private sealed class FakeBlacklist(string[] ssns) : ILoanBlacklist
    {
        private readonly HashSet<string> _ssns = ssns.Select(Ssn.Normalize).ToHashSet();

        public bool Contains(string normalizedSsn) => _ssns.Contains(normalizedSsn);
    }
}
