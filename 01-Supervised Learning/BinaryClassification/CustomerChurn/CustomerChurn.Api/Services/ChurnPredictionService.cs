using CustomerChurn.Api.Services.Contract;
using CustomerChurn.Training.Models;
using Microsoft.Extensions.ML;

namespace CustomerChurn.Api.Services
{
    public class ChurnPredictionService : IChurnPredictionService
    {
        private readonly PredictionEnginePool<ChurnData, CustomerPrediction> _predictionEnginePool;

        public ChurnPredictionService(PredictionEnginePool<ChurnData, CustomerPrediction> predictionEnginePool)
        {
            _predictionEnginePool = predictionEnginePool;
        }

        public async Task<CustomerPrediction> PredictAsync(ChurnData customerData, CancellationToken ct)
        {
            // Bail early if the client drops the request
            ct.ThrowIfCancellationRequested();

            // Intercept pool configurations here if custom probability thresholds are required
            return _predictionEnginePool.Predict("CustomerChurnModel", customerData);
        }
    }
}
