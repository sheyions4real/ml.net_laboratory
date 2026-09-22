using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using MLNet.Shared.Dtos;

namespace CreditCardFraudDetection.Api.Services
{
    public class FraudPredictionService : IFraudPredictionService
    {
        private readonly IFraudPredictionEngine _predictionEngine;
        // Hardcode the 0.40 threshold sweet spot you discovered during evaluation
        private const float FraudProbabilityThreshold = 0.40f;
        public FraudPredictionService(IFraudPredictionEngine predictionEngine)
        {
            _predictionEngine = predictionEngine;
        }
        public async Task<PredictionResultDto<bool>> PredictFraudAsync(TransactionData transaction, CancellationToken cancellationToken)
        {
            var prediction = _predictionEngine.Predict(modelName: "FraudModel", example: transaction);

            // 2. Override the default 0.50 flag with your optimized 0.40 threshold!
            prediction.Prediction = prediction.Probability >= FraudProbabilityThreshold;


            return new PredictionResultDto<bool>
            {
                Prediction = prediction.Prediction,
                Confidence = prediction.Probability
            };
        }
    }
}
