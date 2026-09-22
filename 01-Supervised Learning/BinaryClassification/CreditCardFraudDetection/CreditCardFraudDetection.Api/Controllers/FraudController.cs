using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using Microsoft.AspNetCore.Mvc;
using MLNet.Shared.Dtos;

namespace CreditCardFraudDetection.Api.Controllers;


[ApiController]
[Route("api/v1/[controller]")]
public class FraudController : ControllerBase
{
    private readonly IFraudPredictionService _fraudPredictionService;
    public FraudController(IFraudPredictionService fraudPredictionService) 
    {
        _fraudPredictionService = fraudPredictionService;
    }

    [HttpPost("predict")]
    [ProducesResponseType(typeof(PredictionResultDto<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PredictAsync([FromBody] TransactionData transaction, CancellationToken cancellationToken)
    {
        if (transaction == null)
            return BadRequest("Invalid transaction Data");
        var result = _fraudPredictionService.PredictFraudAsync(transaction, cancellationToken);
        return Ok(result);
    }


}
