using CustomerChurn.Training.Models;
using Microsoft.ML;
using Microsoft.ML.AutoML;

class Program
{
    public static void Main(string[] args)
    {
        var mlContext = new MLContext(seed: 42);
        string dataPath = Path.Combine(Environment.CurrentDirectory, "data", "customer_churn.csv");
        if (!File.Exists(dataPath))
        {
            throw new FileNotFoundException($"Training file not found {dataPath}");
        }


        //Load the Data
        IDataView dataView = mlContext.Data.LoadFromTextFile<ChurnData>(dataPath, hasHeader:true, separatorChar: ',');
        var trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // data preparation

        /* to replace missing values if any
         * var dataPrepPipeline = mlContext.Transforms.ReplaceMissingValues(
            new[] {
                new InputOutputColumnPair("MonthlyCharges", "MonthlyCharges"),
                new InputOutputColumnPair("TotalCharges", "TotalCharges")
            }, replacementMode: Microsoft.ML.Transforms.MissingValueReplacingEstimator.ReplacementMode.Mean)
        .Append(mlContext.Transforms.Categorical.OneHotEncoding(...));
         
         */

        var dataPrepPipeline = mlContext.Transforms.Categorical.OneHotEncoding(new[] {
                new InputOutputColumnPair("ContractTypeEncoded","ContractType"),
                new InputOutputColumnPair("PaymentMethodEncoded", "PaymentMethod"),
                new InputOutputColumnPair("InternetServiceEncoded", "InternetService"),
                new InputOutputColumnPair("OnlineSecurityEncoded", "OnlineSecurity"),
                new InputOutputColumnPair("TechSupportEncoded", "TechSupport"),
                new InputOutputColumnPair("StreamingTVEncoded", "StreamingTV")
            })
            // 1. Handle potential missing values that disrupt numerical training
            .Append(mlContext.Transforms.ReplaceMissingValues(
                new[] { new InputOutputColumnPair("TotalCharges", "TotalCharges") },
                replacementMode: Microsoft.ML.Transforms.MissingValueReplacingEstimator.ReplacementMode.Mean))
            .Append(mlContext.Transforms.Concatenate("Features",
            "TenureMonths", "MonthlyCharges", "TotalCharges", "SeniorCitizen", "Partner", "Dependents", "NumSupportTickets",
            "ContractTypeEncoded", "PaymentMethodEncoded", "InternetServiceEncoded", "OnlineSecurityEncoded", "TechSupportEncoded", "StreamingTVEncoded"
            ));


        // ==========================================
        // APPROACH A: Baseline Model (LightGBM)
        // ==========================================
        Console.WriteLine("--- Training Baseline LightGBM Model ---");
        var baseLinePipeline = dataPrepPipeline.Append(mlContext.BinaryClassification.Trainers.LightGbm(new Microsoft.ML.Trainers.LightGbm.LightGbmBinaryTrainer.Options
        {
            // Drastically reduce tree size to prevent memorization
        NumberOfLeaves = 4,              // Down from 31
        MinimumExampleCountPerLeaf = 40, // Up from 20 (forces larger groups per leaf)
        LearningRate = 0.01,             // Slower learning rate for smoother convergence
        NumberOfIterations = 30,         // Fewer trees to prevent late-stage overfitting
        }));

        // Training first model
        var baseLineModel = baseLinePipeline.Fit(trainTestSplit.TrainSet);
        var baseModelTrainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, baseLineModel);
        var baseModelResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, baseLineModel);
        EvaluationEngine.PrintEvaluationMetrics(baseModelResults.AlgorithmName, baseModelTrainResults, baseModelResults);
        Console.WriteLine($"Baseline Model: {baseModelResults.AlgorithmName} \nF1-Score: {baseModelResults.Metrics["F1Score"]:F4}");



        // trying other models
        // Alternative option: FastTree with small depth
        var fastTreePipeline = dataPrepPipeline.Append(
            mlContext.BinaryClassification.Trainers.FastTree(
                new Microsoft.ML.Trainers.FastTree.FastTreeBinaryTrainer.Options
                {
                    NumberOfLeaves = 6,
                    NumberOfTrees = 50,
                    MinimumExampleCountPerLeaf = 25
                }));

        var baseLineModel_1 = fastTreePipeline.Fit(trainTestSplit.TrainSet);
        var baseModelTrainResults_1 = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, baseLineModel_1);
        var baseModelResults_1 = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, baseLineModel_1);
        EvaluationEngine.PrintEvaluationMetrics(baseModelResults_1.AlgorithmName, baseModelTrainResults_1, baseModelResults_1);
        Console.WriteLine($"Baseline Model: {baseModelResults_1.AlgorithmName} \nF1-Score: {baseModelResults_1.Metrics["F1Score"]:F4}");


        // Alternative option: LBFGS Logistic Regression (Linear Model)
        var linearPipeline = dataPrepPipeline.Append(
            mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                new Microsoft.ML.Trainers.LbfgsLogisticRegressionBinaryTrainer.Options
                {
                    L1Regularization = 0.45f,
                    L2Regularization = 0.45f
                }));
        var baseLineModel_2 = linearPipeline.Fit(trainTestSplit.TrainSet);
        var baseModelTrainResults_2 = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, baseLineModel_2);
        var baseModelResults_2 = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, baseLineModel_2);
        EvaluationEngine.PrintEvaluationMetrics(baseModelResults_2.AlgorithmName, baseModelTrainResults_2, baseModelResults_2);
        Console.WriteLine($"Baseline Model: {baseModelResults_2.AlgorithmName} \nF1-Score: {baseModelResults_2.Metrics["F1Score"]:F4}");



        // ==========================================
        // APPROACH B: AutoML Hyperparameter Tuning
        // ==========================================
        uint trainingTime = 180;
        Console.WriteLine($"\n--- Starting AutoML Experiment ({trainingTime} Seconds) ---");

        // Train the data prep pipeline once so we can use it to transform data and recombine later
        var trainedDataPrepModel = dataPrepPipeline.Fit(trainTestSplit.TrainSet);


        var transformedTrainData = dataPrepPipeline.Fit(trainTestSplit.TrainSet).Transform(trainTestSplit.TrainSet);
        var transformedTestData = dataPrepPipeline.Fit(trainTestSplit.TrainSet).Transform(trainTestSplit.TestSet);

        var experimentSettings = new BinaryExperimentSettings
        {
            MaxExperimentTimeInSeconds = trainingTime,
            OptimizingMetric = BinaryClassificationMetric.F1Score,
        };

        var experiment = mlContext.Auto().CreateBinaryClassificationExperiment(experimentSettings);
        var automlRun = experiment.Execute(transformedTrainData, transformedTestData, labelColumnName: "Label");

        var bestRun = automlRun.BestRun;
        Console.WriteLine($"Best AutoML Trainer: {bestRun.TrainerName}");

        // Re-construct whole pipeline for the AutoML winner to preserve data transformations inside the zip file
        var automlModel = trainedDataPrepModel.Append(bestRun.Model);
        var automlModelTrainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, automlModel, threshold: 0.50f);
        var automlModelResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, automlModel, threshold: 0.50f);

        EvaluationEngine.PrintEvaluationMetrics(automlModelTrainResults.AlgorithmName, automlModelTrainResults, automlModelResults);

        Console.WriteLine($"AutoML: {automlModelResults.AlgorithmName}\n F1-Score: {automlModelResults.Metrics["F1Score"]:F4}");

        // ==========================================
        // EVALUATE & SAVE THE CHAMPION MODEL
        // ==========================================
        ITransformer finalModel = baseModelResults.Metrics["F1Score"] >= automlModelResults.Metrics["F1Score"]
            ? baseLineModel
            : automlModel;

        Console.WriteLine($"\nChoosing model for deployment. Best Test F1: {Math.Max(baseModelResults.Metrics["F1Score"], automlModelResults.Metrics["F1Score"]):F4}");

        // Deploy Paths
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string projectRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
        string modelDeployPath = Path.Combine(projectRoot, "model", "CustomerChurnModel.zip");

        Directory.CreateDirectory(Path.GetDirectoryName(modelDeployPath)!);
        mlContext.Model.Save(finalModel, trainTestSplit.TrainSet.Schema, modelDeployPath);
        Console.WriteLine($"Model deployed successfully to: {modelDeployPath}");


    }

}