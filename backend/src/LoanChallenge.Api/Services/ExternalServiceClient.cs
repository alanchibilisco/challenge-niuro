using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoanChallenge.Api.Services;

/// <summary>
/// Cliente HTTP hacia el servicio externo simulado. El contrato es idempotente:
/// un PUT a /api/customers/{ssn} crea o actualiza el registro con esa clave natural,
/// de modo que reintentar un envío nunca duplica datos.
/// </summary>
public sealed class ExternalServiceClient(HttpClient http)
{
    public async Task<ExternalServiceResponse> SendCustomerUpdateAsync(string ssn, string payloadJson, CancellationToken cancellationToken)
    {
        string url = $"api/customers/{Uri.EscapeDataString(ssn)}";
        using StringContent content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await http.PutAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        string stringContent=await response.Content.ReadAsStringAsync();
        ExternalServiceResponse? externalServiceResponse=JsonSerializer.Deserialize<ExternalServiceResponse>(stringContent);
        
        if (externalServiceResponse==null)
        {
            throw new Exception("No se pudo deserializar la respuesta del servicio externo.");
        }

        return externalServiceResponse;
    }
}

public sealed class ExternalServiceResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("ssn")]
    public required string Ssn { get; set; }
    [JsonPropertyName("operation")]
    public required string Operation { get; set; }
}