using LoanChallenge.Core.Domain;

namespace LoanChallenge.Core.Application;

/// <summary>
/// Acceso a datos transaccional. La implementación de infraestructura (EF Core)
/// garantiza que cliente + solicitud + evento outbox se guarden en un único
/// <c>SaveChanges</c> (una sola transacción real).
/// </summary>
public interface ILoanRepository
{
    Task<Customer?> FindCustomerBySsnAsync(string ssn, CancellationToken cancellationToken);

    Task<LoanApplication?> FindApplicationByCustomerIdAsync(int customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserta o actualiza el cliente y la solicitud, y publica el evento outbox,
    /// todo en una única transacción: si algo falla, no queda nada persistido.
    /// </summary>
    // Task SaveAsync(
    //     Customer customer,
    //     LoanApplication application,
    //     string outboxPayload,
    //     CancellationToken cancellationToken);
    Task<(Customer customer, LoanApplication application)> SaveAsync(
        Customer customer,
        LoanApplication application,
        string outboxPayload,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ejecuta el action dentro de una única transacción: si algo falla, no queda nada persistido.
    /// </summary>
    Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken);
}
