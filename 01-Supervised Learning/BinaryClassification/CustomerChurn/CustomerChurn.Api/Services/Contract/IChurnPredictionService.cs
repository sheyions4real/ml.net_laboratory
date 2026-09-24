using CustomerChurn.Training.Models;

namespace CustomerChurn.Api.Services.Contract;

public interface IChurnPredictionService
{
    Task<CustomerPrediction> PredictAsync(ChurnData customerData, CancellationToken ct);
}
