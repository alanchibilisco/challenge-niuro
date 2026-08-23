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
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression(@"^[\p{L} ]{2,50}$",ErrorMessage ="El estado solo puede contener letras y espacios.")]
    public string State { get; set; } = "";

    [Required]
    [StringLength(150)]
    public string CompanyName { get; set; } = "";

    [Range(1, 10_000_000_000, ErrorMessage ="El monto solicitado debe ser entre 1 y 10.000.000.000")]
    public decimal RequestedAmount { get; set; }

    [Required]
    [RegularExpression(@"^\d{8,10}$", ErrorMessage = "El SSN debe tener entre 8 y 9 dígitos.")]
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
