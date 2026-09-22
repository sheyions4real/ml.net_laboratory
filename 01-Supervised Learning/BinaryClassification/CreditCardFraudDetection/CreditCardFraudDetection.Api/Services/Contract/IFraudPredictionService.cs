
using CreditCardFraudDetection.Training.Models;
using MLNet.Shared.Dtos;

namespace CreditCardFraudDetection.Api.Services.Contract;

public interface IFraudPredictionService
{
    Task<PredictionResultDto<bool>> PredictFraudAsync(TransactionData transaction, CancellationToken cancellationToken);
}
