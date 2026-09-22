using CreditCardFraudDetection.Training.Models;

namespace CreditCardFraudDetection.Api.Services.Contract;

public interface IFraudPredictionEngine
{
    TransactionPrediction Predict(string modelName, TransactionData example);
}
