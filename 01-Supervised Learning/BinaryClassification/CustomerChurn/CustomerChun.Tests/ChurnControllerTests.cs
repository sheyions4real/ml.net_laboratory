using CustomerChurn.Api.Controllers;
using CustomerChurn.Api.Services.Contract;
using CustomerChurn.Training.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CustomerChun.Tests
{
    public class ChurnControllerTests
    {
        [Fact]
        public async Task PredictAsync_ReturnsOkResult_WithValidPrediction()
        {
            // Arrange
            var mockService = new Mock<IChurnPredictionService>();
            var fakePrediction = new CustomerPrediction { Prediction = true, Probability = 0.87f };

            // Use ReturnsAsync instead of Returns
            mockService.Setup(s => s.PredictAsync(It.IsAny<ChurnData>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(fakePrediction);

            var controller = new ChurnController(mockService.Object);

            // Act
            var result = await controller.PredictAsync(new ChurnData(), CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedModel = Assert.IsType<CustomerPrediction>(okResult.Value);
            Assert.True(returnedModel.Prediction);
        }
    }
}
