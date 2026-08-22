using System.Net;
using System.Net.Http.Json;
using LoanChallenge.Api.Data;
using LoanChallenge.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanChallenge.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Solicitud_valida_se_aprueba_y_persiste_cliente_solicitud_y_evento()
    {
        await _factory.ResetDatabaseAsync();
        HttpResponseMessage response = await SubmitAsync(ValidPayload());

        response.EnsureSuccessStatusCode();
        SubmitResultJson? result = await response.Content.ReadFromJsonAsync<SubmitResultJson>();
        Assert.Equal("Approved", result!.Status);
        Assert.True(result.IsNewCustomer);

        await using LoanDbContext db = _factory.CreateDbContext();
        Assert.Single(db.Customers);
        Assert.Single(db.Applications);
        Assert.Single(db.OutboxMessages);
        Assert.Equal(OutboxStatuses.Pending, db.OutboxMessages.Single().Status);
        Assert.True(db.Applications.Single().CustomerId == db.Customers.Single().Id);
    }

    [Fact]
    public async Task Estado_NY_se_deniega_y_no_persiste_nada()
    {
        await _factory.ResetDatabaseAsync();
        ValidPayloadJson payload = ValidPayload() with { State = "NY" };

        HttpResponseMessage response = await SubmitAsync(payload);

        response.EnsureSuccessStatusCode();
        SubmitResultJson? result = await response.Content.ReadFromJsonAsync<SubmitResultJson>();
        Assert.Equal("Denied", result!.Status);
        Assert.Equal("ny_state", result.DenialCode);

        await using LoanDbContext db = _factory.CreateDbContext();
        Assert.Empty(db.Customers);
        Assert.Empty(db.Applications);
        Assert.Empty(db.OutboxMessages);
    }

    [Fact]
    public async Task Ssn_en_lista_negra_se_deniega()
    {
        await _factory.ResetDatabaseAsync();
        ValidPayloadJson payload = ValidPayload() with { Ssn = "111-11-1111" };

        HttpResponseMessage response = await SubmitAsync(payload);

        response.EnsureSuccessStatusCode();
        SubmitResultJson? result = await response.Content.ReadFromJsonAsync<SubmitResultJson>();
        Assert.Equal("Denied", result!.Status);
        Assert.Equal("ssn_blacklisted", result.DenialCode);

        await using LoanDbContext db = _factory.CreateDbContext();
        Assert.Empty(db.Customers);
    }

    [Fact]
    public async Task Cliente_recurrente_actualiza_en_vez_de_crear_duplicados()
    {
        await _factory.ResetDatabaseAsync();
        ValidPayloadJson first = ValidPayload();
        HttpResponseMessage firstResponse = await SubmitAsync(first);
        SubmitResultJson? firstResult = await firstResponse.Content.ReadFromJsonAsync<SubmitResultJson>();

        ValidPayloadJson second = first with { RequestedAmount = 25_000m, LastName = "Gomez-Lopez" };
        HttpResponseMessage secondResponse = await SubmitAsync(second);
        SubmitResultJson? secondResult = await secondResponse.Content.ReadFromJsonAsync<SubmitResultJson>();

        Assert.Equal("Approved", secondResult!.Status);
        Assert.False(secondResult.IsNewCustomer);
        Assert.Equal(firstResult!.CustomerId, secondResult.CustomerId);
        Assert.Equal(firstResult.ApplicationId, secondResult.ApplicationId);

        await using LoanDbContext db = _factory.CreateDbContext();
        Assert.Single(db.Customers);
        Assert.Single(db.Applications);
        Assert.Equal(2, db.OutboxMessages.Count());

        Customer customer = await db.Customers.SingleAsync();
        LoanApplication application = await db.Applications.SingleAsync();
        Assert.Equal("Gomez-Lopez", customer.LastName);
        Assert.Equal(25_000m, application.RequestedAmount);
    }

    [Fact]
    public async Task Ssn_con_y_sin_guiones_identifica_al_mismo_cliente()
    {
        await _factory.ResetDatabaseAsync();
        ValidPayloadJson first = ValidPayload() with { Ssn = "444-55-6666" };
        await SubmitAsync(first);

        ValidPayloadJson second = ValidPayload() with { Ssn = "444556666", RequestedAmount = 50_000m };
        HttpResponseMessage secondResponse = await SubmitAsync(second);
        SubmitResultJson? secondResult = await secondResponse.Content.ReadFromJsonAsync<SubmitResultJson>();

        Assert.False(secondResult!.IsNewCustomer);

        await using LoanDbContext db = _factory.CreateDbContext();
        Assert.Single(db.Customers);
        Assert.Equal(50_000m, (await db.Applications.SingleAsync()).RequestedAmount);
    }

    [Fact]
    public async Task Campos_invalidos_devuelven_400()
    {
        await _factory.ResetDatabaseAsync();
        ValidPayloadJson invalid = ValidPayload() with { State = "", Ssn = "123" };

        HttpResponseMessage response = await SubmitAsync(invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using LoanDbContext db = _factory.CreateDbContext();
        Assert.Empty(db.Customers);
    }

    private Task<HttpResponseMessage> SubmitAsync(ValidPayloadJson payload) =>
        _client.PostAsJsonAsync("/api/loan-applications", payload);

    private static ValidPayloadJson ValidPayload() => new()
    {
        FirstName = "Ana",
        LastName = "Gomez",
        Address = "123 Main St, San Francisco",
        State = "CA",
        CompanyName = "Acme Inc",
        RequestedAmount = 10_000m,
        Ssn = "333-33-3333",
    };

    public record ValidPayloadJson
    {
        public string FirstName { get; init; } = "";
        public string LastName { get; init; } = "";
        public string Address { get; init; } = "";
        public string State { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public decimal RequestedAmount { get; init; }
        public string Ssn { get; init; } = "";
    }

    private sealed record SubmitResultJson(
        string Status,
        int? CustomerId,
        int? ApplicationId,
        bool IsNewCustomer,
        string? DenialCode,
        string? DenialReason);
}
