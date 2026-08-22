using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Simulación del servicio externo: almacena los clientes recibidos en memoria.
// El SSN es la clave natural: un PUT crea el registro si no existe y lo actualiza
// si ya existe, lo que hace que los reintentos del backend sean idempotentes.
var customers = new ConcurrentDictionary<string, ExternalCustomer>(StringComparer.Ordinal);

app.MapPut("/api/customers/{ssn}", (string ssn, ExternalCustomerPayload payload) =>
{
    var customer = new ExternalCustomer(
        Ssn: ssn,
        FirstName: payload.FirstName,
        LastName: payload.LastName,
        Address: payload.Address,
        State: payload.State,
        CompanyName: payload.CompanyName,
        RequestedAmount: payload.RequestedAmount,
        IsNewCustomer: payload.IsNewCustomer,
        UpdatedAt: DateTime.UtcNow);

    var created = customers.TryAdd(ssn, customer);
    if (!created)
    {
        customers[ssn] = customer;
    }

    var operation = created ? "ALTA" : "ACTUALIZACION";
    app.Logger.LogInformation(
        "Recibida {Operation} para SSN {Ssn}: {FirstName} {LastName} - {CompanyName} - ${RequestedAmount}",
        operation, ssn, customer.FirstName, customer.LastName, customer.CompanyName, customer.RequestedAmount);

    return Results.Ok(new { ssn, created });
});

app.MapGet("/api/customers", () =>
    customers.Values.OrderByDescending(c => c.UpdatedAt));

app.Run();

public record ExternalCustomerPayload(
    string? FirstName,
    string? LastName,
    string? Address,
    string? State,
    string? CompanyName,
    string? Ssn,
    decimal? RequestedAmount,
    bool IsNewCustomer);

public record ExternalCustomer(
    string Ssn,
    string? FirstName,
    string? LastName,
    string? Address,
    string? State,
    string? CompanyName,
    decimal? RequestedAmount,
    bool IsNewCustomer,
    DateTime UpdatedAt);
