namespace LoanChallenge.Core.Domain;

public class LoanApplication
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public decimal RequestedAmount { get; set; }

    public DateTime CreatedAt { get; set; }=DateTime.UtcNow;
}
