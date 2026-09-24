using CustomerChurn.Api.Services;
using CustomerChurn.Api.Services.Contract;
using CustomerChurn.Training.Models;
using Microsoft.Extensions.ML;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

string modelPath = Path.Combine(builder.Environment.ContentRootPath, "..", "model", "CustomerChurnModel.zip");


// Add services to the container.
builder.Services.AddPredictionEnginePool<ChurnData, CustomerPrediction>()
    .FromFile(modelName: "CustomerChurnModel", filePath: modelPath, watchForChanges: true);

builder.Services.AddScoped<IChurnPredictionService, ChurnPredictionService>();


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
