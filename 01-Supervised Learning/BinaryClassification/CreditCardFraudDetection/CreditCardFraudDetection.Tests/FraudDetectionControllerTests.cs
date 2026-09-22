using CreditCardFraudDetection.Api.Controllers;
using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using Microsoft.AspNetCore.Mvc;
using MLNet.Shared.Dtos;
using Moq;
using Xunit;

namespace CreditCardFraudDetection.Api.Tests;

public class FraudDetectionControllerTests
{
    private readonly Mock<IFraudPredictionService> _mockFraudPredictionService;
    private readonly FraudPredictionController _controller;

    public FraudDetectionControllerTests()
    {
        _mockFraudPredictionService = new Mock<IFraudPredictionService>();
        _controller = new FraudPredictionController(_mockFraudPredictionService.Object);
    }

    [Fact]
    public async Task Predict_ReturnsOkResult_WithValidPayloadAsync()
    {
        // Arrange
        var requestData = new TransactionData { TransactionAmount = 500f };
        var expectedPrediction = new PredictionResultDto<bool> { Prediction = true, Confidence = 0.85f };

        _mockFraudPredictionService
            .Setup(service => service.PredictFraudAsync(requestData, CancellationToken.None))
            .ReturnsAsync(expectedPrediction);

        // Act
        var response = await _controller.PredictAsync(requestData, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(response);
        var returnedPrediction = Assert.IsType<PredictionResultDto<bool>>(okResult.Value);

        Assert.True(returnedPrediction.Prediction);
        Assert.Equal(0.85f, returnedPrediction.Confidence);
    }

    [Fact]
    public async Task Predict_ReturnsBadRequest_WhenPayloadIsNull()
    {
        // Act
        var response = await _controller.PredictAsync(null!, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Invalid transaction Data", badRequestResult.Value);
    }
}
