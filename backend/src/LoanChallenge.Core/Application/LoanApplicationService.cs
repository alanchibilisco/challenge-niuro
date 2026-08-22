using System.Text.Json;
using LoanChallenge.Core.Domain;
using LoanChallenge.Core.Domain.Rules;
using Microsoft.Extensions.Logging;

namespace LoanChallenge.Core.Application;

/// <summary>
/// Caso de uso: solicitud de préstamo. Decide con el motor de reglas y, si aprueba,
/// persiste cliente + solicitud + evento outbox como una sola unidad de trabajo.
/// Un mismo SSN identifica a un único cliente y a una única solicitud: si ya existen,
/// se actualizan en lugar de insertar de nuevo.
/// </summary>
public sealed class LoanApplicationService(LoanRulesEngine rulesEngine, ILoanRepository repository, ILogger<LoanApplicationService> logger)
{
    private static readonly JsonSerializerOptions EventJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<SubmitResult> SubmitAsync(LoanRequest request, CancellationToken cancellationToken)
    {
        RuleDecision decision = rulesEngine.Decide(request);
        if (!decision.IsApproved)
        {
            return SubmitResult.Denied(decision.DenialCode!, decision.DenialReason!);
        }

        string normalizedSsn = Ssn.Normalize(request.Ssn);
        Customer? customer = await repository.FindCustomerBySsnAsync(normalizedSsn, cancellationToken);
        bool isNewCustomer = customer is null;

        int customerId = 0;
        int applicationId = 0;

        LoanApplication application;

        Func<Task> action = async () =>
        {
            if (customer is null)
            {
                customer = new Customer
                {
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Address = request.Address,
                    State = request.State,
                    CompanyName = request.CompanyName,
                    Ssn = normalizedSsn,
                };
                application = new LoanApplication
                {
                    CreatedAt = DateTime.UtcNow,
                    RequestedAmount = request.RequestedAmount,
                };
            }
            else
            {
                customer.FirstName = request.FirstName;
                customer.LastName = request.LastName;
                customer.Address = request.Address;
                customer.State = request.State;
                customer.CompanyName = request.CompanyName;

                application = await repository.FindApplicationByCustomerIdAsync(customer.Id, cancellationToken)
                    ?? throw new InvalidOperationException($"No existe una solicitud para el cliente {customer.Id}.");
                application.RequestedAmount = request.RequestedAmount;
            }

            LoanApprovalEvent approvalEvent = new LoanApprovalEvent(
                request.FirstName,
                request.LastName,
                request.Address,
                request.State,
                request.CompanyName,
                normalizedSsn,
                request.RequestedAmount,
                isNewCustomer);

            (Customer customerSaved, LoanApplication applicationSaved) = await repository.SaveAsync(
                customer,
                application,
                JsonSerializer.Serialize(approvalEvent, EventJsonOptions),
                cancellationToken);

            customerId = customerSaved.Id;
            applicationId = applicationSaved.Id;

        };
        try
        {
            await repository.ExecuteTransactionAsync(action, cancellationToken);
            return SubmitResult.Approved(customerId, applicationId, isNewCustomer);
        }
        catch (System.Exception e)
        {            
            logger.LogError($"Error al procesar solicitud de préstamo para SSN {normalizedSsn}: {e.Message}",e);
            
            return SubmitResult.Denied(
                "INTERNAL_ERROR",
                "Ocurrió un error al procesar la solicitud.");
        }

    }
}
