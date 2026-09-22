using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using Microsoft.Extensions.ML;

namespace CreditCardFraudDetection.Api.Services;

public class FraudPredictionEngineAdapter : IFraudPredictionEngine
{
    private readonly PredictionEnginePool<TransactionData, TransactionPrediction> _pool;

    public FraudPredictionEngineAdapter(PredictionEnginePool<TransactionData, TransactionPrediction> pool)
    {
        _pool = pool;
    }

    public TransactionPrediction Predict(string modelName, TransactionData example) =>
        _pool.Predict(modelName, example);
}
