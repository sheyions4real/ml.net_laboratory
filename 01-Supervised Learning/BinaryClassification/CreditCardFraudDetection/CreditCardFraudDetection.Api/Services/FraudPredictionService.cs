using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using Microsoft.Extensions.ML;
using MLNet.Shared.Dtos;

namespace CreditCardFraudDetection.Api.Services
{
    public class FraudPredictionService : IFraudPredictionService
    {
        private readonly PredictionEnginePool<TransactionData, TransactionPrediction> _predictionEnginePool;

        public FraudPredictionService(PredictionEnginePool<TransactionData, TransactionPrediction> predictionEnginePool)
        {
            _predictionEnginePool = predictionEnginePool;
        }
        public async Task<PredictionResultDto<bool>> PredictFraudAsync(TransactionData transaction, CancellationToken cancellationToken)
        {
            var prediction = _predictionEnginePool.Predict(modelName: "FraudModel", example: transaction);
           
            return new PredictionResultDto<bool>
            {
                Prediction = prediction.Prediction,
                Confidence = prediction.Probability
            };
        }
    }
}
