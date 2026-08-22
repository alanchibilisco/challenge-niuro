using LoanChallenge.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoanChallenge.Api.Data;

public class LoanDbContext(DbContextOptions<LoanDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<LoanApplication> Applications => Set<LoanApplication>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasIndex(c => c.Ssn).IsUnique();
        });

        modelBuilder.Entity<LoanApplication>(entity =>
        {
            entity.ToTable("LoanApplications");
            entity.Property(a => a.RequestedAmount).HasPrecision(18, 2);
            entity.HasOne(a => a.Customer)
                .WithMany()
                .HasForeignKey(a => a.CustomerId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasIndex(m => m.Status);
        });
    }
}
