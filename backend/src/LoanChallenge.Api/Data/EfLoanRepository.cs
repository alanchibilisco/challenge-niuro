using LoanChallenge.Core.Application;
using LoanChallenge.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanChallenge.Api.Data;

/// <summary>
/// Implementación EF Core de <see cref="ILoanRepository"/>.
/// <see cref="SaveAsync"/> agrega (o rastrea la modificación de) el cliente, la
/// solicitud y el mensaje outbox, y los persiste con un único <c>SaveChanges</c>,
/// que SQLite ejecuta como una única transacción: o se guardan los tres o ninguno.
/// </summary>
public class EfLoanRepository(LoanDbContext db) : ILoanRepository
{
    public Task<Customer?> FindCustomerBySsnAsync(string ssn, CancellationToken cancellationToken) =>
        db.Customers.FirstOrDefaultAsync(c => c.Ssn == ssn, cancellationToken);

    public Task<LoanApplication?> FindApplicationByCustomerIdAsync(int customerId, CancellationToken cancellationToken) =>
        db.Applications.FirstOrDefaultAsync(a => a.CustomerId == customerId, cancellationToken);

    public async Task<(Customer customer, LoanApplication application)> SaveAsync(
        Customer customer,
        LoanApplication application,
        string outboxPayload,
        CancellationToken cancellationToken)
    {

        if (customer.Id == 0)
        {
            db.Customers.Add(customer);
            if (application.CustomerId == 0)
            {
                application.Customer = customer;
            }
        }

        if (application.Id == 0)
        {
            db.Applications.Add(application);
        }

        db.OutboxMessages.Add(new OutboxMessage
        {
            Ssn = customer.Ssn,
            Payload = outboxPayload,
            Status = OutboxStatuses.Pending,
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);

        return (customer, application);

    }

    public async Task ExecuteTransactionAsync(Func<Task> action, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action();
            await transaction.CommitAsync(cancellationToken);
        }
        catch (System.Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
