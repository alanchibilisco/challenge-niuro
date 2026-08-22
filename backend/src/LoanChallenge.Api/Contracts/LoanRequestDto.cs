using System.ComponentModel.DataAnnotations;
using LoanChallenge.Core.Domain;

namespace LoanChallenge.Api.Contracts;

public sealed class LoanRequestDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    [StringLength(300)]
    public string Address { get; set; } = "";

    [Required]
    [StringLength(2, MinimumLength = 2)]
    [RegularExpression("^[A-Z]{2}$")]
    public string State { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string CompanyName { get; set; } = "";

    [Range(1, 10_000_000_000)]
    public decimal RequestedAmount { get; set; }

    [Required]
    [RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "El SSN debe tener 9 dígitos (p. ej. 123-45-6789).")]
    public string Ssn { get; set; } = "";

    public LoanRequest ToRequest() => new(
        FirstName.Trim(),
        LastName.Trim(),
        Address.Trim(),
        State.Trim(),
        CompanyName.Trim(),
        RequestedAmount,
        Ssn.Trim());
}
