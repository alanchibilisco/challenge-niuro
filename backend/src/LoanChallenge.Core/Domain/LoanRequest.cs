namespace LoanChallenge.Core.Domain;

public sealed record LoanRequest(
    string FirstName,
    string LastName,
    string Address,
    string State,
    string CompanyName,
    decimal RequestedAmount,
    string Ssn);
