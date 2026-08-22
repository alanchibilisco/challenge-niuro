namespace LoanChallenge.Core.Application;

/// <summary>Evento que se envía al servicio externo cuando una solicitud es aprobada.</summary>
public sealed record LoanApprovalEvent(
    string FirstName,
    string LastName,
    string Address,
    string State,
    string CompanyName,
    string Ssn,
    decimal RequestedAmount,
    bool IsNewCustomer);
