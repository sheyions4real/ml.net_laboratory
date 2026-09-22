using CreditCardFraudDetection.Api.Services;
using CreditCardFraudDetection.Api.Services.Contract;
using CreditCardFraudDetection.Training.Models;
using Microsoft.Extensions.ML;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddPredictionEnginePool<TransactionData, TransactionPrediction>()
    .FromFile(
        modelName: "FraudModel",
        filePath: Path.Combine(AppContext.BaseDirectory, "model", "FraudModel.zip"),
        watchForChanges: true // Auto-reloads instantly when the training app updates the file!
 );

// add dependency injection
builder.Services.AddScoped<IFraudPredictionService, FraudPredictionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();
