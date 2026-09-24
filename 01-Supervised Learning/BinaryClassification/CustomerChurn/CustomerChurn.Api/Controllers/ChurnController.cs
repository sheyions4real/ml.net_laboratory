using CustomerChurn.Api.Services;
using CustomerChurn.Api.Services.Contract;
using CustomerChurn.Training.Models;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.X86;

namespace CustomerChurn.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ChurnController : ControllerBase
    {
        private readonly IChurnPredictionService _churnPredictionService;

        public ChurnController(IChurnPredictionService churnPredictionService)
        {
            _churnPredictionService = churnPredictionService;
        }

        [HttpPost("predict")]
        public async Task<ActionResult<CustomerPrediction>> PredictAsync([FromBody] ChurnData input, CancellationToken cancellationToken=default)
        {
            var prediction = await _churnPredictionService.PredictAsync(input, cancellationToken);
            return Ok(prediction);
        }
    }
}
