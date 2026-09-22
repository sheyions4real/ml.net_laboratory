using CreditCardFraudDetection.Training.Models;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;
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

// regularization for fast tree
// Define options to restrict tree growth and force generalization
var options = new FastTreeBinaryTrainer.Options
{
    LabelColumnName = "Label",
    FeatureColumnName = "Features",

    // 1. Reduce the complexity of each individual tree
    NumberOfLeaves = 15,            // Default is 20. Lowering this limits tree depth.

    // 2. Reduce the total number of trees built
    NumberOfTrees = 50,             // Default is 100. Fewer trees prevents over-memorization.

    // 3. Force trees to only make a rule if a group of rows share it
    MinimumExampleCountPerLeaf = 30,// Default is 10. Requires 30 rows to establish a pattern.

    // 4. Slow down the learning process
    LearningRate = 0.05             // Default is 0.2. A slower rate prevents over-correcting.
};

// choose Algorithm and append to pipeline
var trainer = mlContext.BinaryClassification.Trainers.FastTree(options);
var trainingPipeline = dataProcessPipeline.Append(trainer);

// training the model
Console.WriteLine("Training Fast Tree Model...");
var trainedModel = trainingPipeline.Fit(trainTestSplit.TrainSet);

// 2. Evaluate the Train Set (Add this to compare)
var trainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, trainedModel);


// centralized Evaluation via shared library
var testResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, trainedModel);

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
