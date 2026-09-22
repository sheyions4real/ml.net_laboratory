using CreditCardFraudDetection.Api.Services;
using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using Moq;

namespace CreditCardFraudDetection.Tests;

public class TransactionServiceTests
{
    private readonly Mock<IFraudPredictionEngine> _mockEngine;
    private readonly FraudPredictionService _sut;

    public TransactionServiceTests()
    {
        _mockEngine = new Mock<IFraudPredictionEngine>();
        _sut = new FraudPredictionService(_mockEngine.Object);
    }

    [Fact]
    public async Task PredictFraud_ShouldReturnTrue_WhenProbabilityIsAboveOrEqualThreshold()
    {
        // Arrange
        var sampleData = new TransactionData { TransactionAmount = 3000f };
        var predictionResult = new TransactionPrediction
        {
            Prediction = false,
            Probability = 0.42f
        };

        _mockEngine
            .Setup(engine => engine.Predict("FraudModel", sampleData))
            .Returns(predictionResult);

        // Act
        var result = await _sut.PredictFraudAsync(sampleData, CancellationToken.None);

        // Assert
        Assert.True(result.Prediction, "The service should override the result to TRUE when probability is >= 0.40");
        Assert.Equal(0.42f, result.Confidence);
    }

    [Fact]
    public async Task PredictFraud_ShouldReturnFalse_WhenProbabilityIsBelowThreshold()
    {
        // Arrange
        var sampleData = new TransactionData { TransactionAmount = 20f };
        var predictionResult = new TransactionPrediction
        {
            Prediction = false,
            Probability = 0.25f
        };

        _mockEngine
            .Setup(engine => engine.Predict("FraudModel", sampleData))
            .Returns(predictionResult);

        // Act
        var result = await _sut.PredictFraudAsync(sampleData, CancellationToken.None);

        // Assert
        Assert.False(result.Prediction, "The service should keep FALSE when probability is below 0.40");
        Assert.Equal(0.25f, result.Confidence);
    }
}
