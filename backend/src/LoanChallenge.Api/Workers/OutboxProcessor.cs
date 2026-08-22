using LoanChallenge.Api.Data;
using LoanChallenge.Api.Options;
using LoanChallenge.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LoanChallenge.Api.Workers;

/// <summary>
/// Entrega en segundo plano los eventos outbox al servicio externo (no dentro del
/// request HTTP del formulario). Cada ciclo toma los mensajes pendientes, los envía
/// por HTTP y los marca como procesados. Si el envío falla, el mensaje permanece
/// pendiente y se reintenta en los siguientes ciclos (backoff natural del poll) hasta
/// agotar los intentos. El servicio externo es idempotente por SSN, así que los
/// reintentos son seguros.
/// </summary>
public sealed class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ExternalServiceClient client,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var pollInterval = TimeSpan.FromSeconds(options.Value.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al procesar los mensajes outbox.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoanDbContext>();

        var pending = await db.OutboxMessages
            .Where(m => m.Status == OutboxStatuses.Pending)
            .OrderBy(m => m.Id)
            .Take(20)
            .ToListAsync(stoppingToken);

        foreach (var message in pending)
        {
            try
            {
                await client.SendCustomerUpdateAsync(message.Ssn, message.Payload, stoppingToken);
                message.Status = OutboxStatuses.Processed;
                message.ProcessedAt = DateTime.UtcNow;
                logger.LogInformation("Evento outbox {OutboxId} enviado al servicio externo (SSN {Ssn}).", message.Id, message.Ssn);
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;

                if (message.Attempts >= options.Value.MaxAttempts)
                {
                    message.Status = OutboxStatuses.Failed;
                    logger.LogError(ex, "Evento outbox {OutboxId} falló tras {Attempts} intentos.", message.Id, message.Attempts);
                }
                else
                {
                    logger.LogWarning(ex, "Evento outbox {OutboxId} no enviado (intento {Attempts}); se reintentará.", message.Id, message.Attempts);
                }
            }
        }

        if (pending.Count > 0)
        {
            await db.SaveChangesAsync(stoppingToken);
        }
    }
}
