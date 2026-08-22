using LoanChallenge.Api.Contracts;
using LoanChallenge.Core.Application;
using Microsoft.AspNetCore.Mvc;

namespace LoanChallenge.Api.Controllers;

[ApiController]
[Route("api/loan-applications")]
public class LoanApplicationsController(LoanApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SubmitResult>> Submit(
        [FromBody] LoanRequestDto dto,
        CancellationToken cancellationToken)
    {
        SubmitResult result = await service.SubmitAsync(dto.ToRequest(), cancellationToken);
        return Ok(result);
    }
}
