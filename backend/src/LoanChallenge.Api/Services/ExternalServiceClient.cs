using System.Text;

namespace LoanChallenge.Api.Services;

/// <summary>
/// Cliente HTTP hacia el servicio externo simulado. El contrato es idempotente:
/// un PUT a /api/customers/{ssn} crea o actualiza el registro con esa clave natural,
/// de modo que reintentar un envío nunca duplica datos.
/// </summary>
public sealed class ExternalServiceClient(HttpClient http)
{
    public async Task SendCustomerUpdateAsync(string ssn, string payloadJson, CancellationToken cancellationToken)
    {
        var url = $"api/customers/{Uri.EscapeDataString(ssn)}";
        using var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await http.PutAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
