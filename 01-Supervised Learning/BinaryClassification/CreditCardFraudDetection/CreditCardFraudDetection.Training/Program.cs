using CreditCardFraudDetection.Training.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.ML.Trainers.FastTree;
using Microsoft.ML.Trainers.LightGbm;
using MLNet.Shared;

var mlContext = new MLContext();

// load data
string dataPath = Path.Combine(Environment.CurrentDirectory, "data", "credit_card_fraud_detection.csv");
if (!File.Exists(dataPath)) throw new FileNotFoundException($"Data file missing at: {dataPath}");

IDataView dataView = mlContext.Data.LoadFromTextFile<TransactionData>(dataPath, hasHeader: true, separatorChar: ',');

// train test split
var trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

// Data Preparation pipeline
var dataProcessPipeline = mlContext.Transforms.Categorical.OneHotEncoding("MerchantCategoryEncoded", nameof(TransactionData.MerchantCategory))
    .Append(mlContext.Transforms.Categorical.OneHotEncoding("DeviceTypeEncoded", nameof(TransactionData.DeviceType)))
    .Append(mlContext.Transforms.Concatenate("Features",
    "MerchantCategoryEncoded", "DeviceTypeEncoded",
    nameof(TransactionData.TransactionAmount), nameof(TransactionData.TransactionHour),
    nameof(TransactionData.DistanceFromHome), nameof(TransactionData.IsOnline),
    nameof(TransactionData.IsInternational), nameof(TransactionData.PreviousFraudFlag),
    nameof(TransactionData.AvgTransactionAmount30d), nameof(TransactionData.TransactionCount24h)));

// Define options to restrict tree growth and force generalization
var options = new LightGbmBinaryTrainer.Options
{
    LabelColumnName = "Label",
    FeatureColumnName = "Features",
    UnbalancedSets = true,

    // Allow the trees to be slightly more expressive (up from 5)
    NumberOfLeaves = 8,
    MinimumExampleCountPerLeaf = 15,

    // Give it a few more iterations to map out rules safely
    NumberOfIterations = 40,
    LearningRate = 0.03
};

// choose Algorithm and append to pipeline
var trainer = mlContext.BinaryClassification.Trainers.LightGbm(options);
var trainingPipeline = dataProcessPipeline.Append(trainer);

// training the model
Console.WriteLine("Training Fast Tree Model...");
var trainedModel = trainingPipeline.Fit(trainTestSplit.TrainSet);

// 2. Evaluate the Train Set (Add this to compare)
var trainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, trainedModel, threshold: 0.40f);


// centralized Evaluation via shared library
var testResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, trainedModel, threshold: 0.40f);

Console.WriteLine($"\n--- Evaluation Metrics ({testResults.AlgorithmName}) ---");
foreach (var metric in testResults.Metrics)
{
    Console.WriteLine($"{metric.Key}: {metric.Value:F4}");
}

// 3. Print side-by-side comparison
Console.WriteLine($"\n--- Metric Comparison ({testResults.AlgorithmName}) ---");
Console.WriteLine($"Metric                 | Train Set | Test Set");
Console.WriteLine($"-----------------------------------------");

foreach (var metric in testResults.Metrics)
{
    double trainValue = trainResults.Metrics.ContainsKey(metric.Key) ? trainResults.Metrics[metric.Key] : 0.0;
    Console.WriteLine($"{metric.Key,-22} | {trainValue:F4}    | {metric.Value:F4}");
}

// Save Model
string modelPath = Path.Combine(Environment.CurrentDirectory, "model", "FraudModel.zip");
Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
mlContext.Model.Save(trainedModel, trainTestSplit.TrainSet.Schema, modelPath);
Console.WriteLine($"\nModel successfully saved to: {modelPath}");

// save in the model folder outside the project
string baseDir = AppDomain.CurrentDomain.BaseDirectory;
string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));

string modelFolder = Path.Combine(projectRoot, "model");
string modelDeployPath = Path.Combine(modelFolder, "FraudModel.zip");

Directory.CreateDirectory(modelFolder);
mlContext.Model.Save(trainedModel, trainTestSplit.TrainSet.Schema, modelDeployPath);
Console.WriteLine($"\nModel successfully saved to project source: {modelDeployPath}");
