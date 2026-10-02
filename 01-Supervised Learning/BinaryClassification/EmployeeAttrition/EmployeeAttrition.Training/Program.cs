using EmployeeAttrition.Training.Models;
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.AutoML;
using static Microsoft.ML.BinaryClassificationCatalog;

class Program
{
    public static void Main(string[] args)
    {
        // create ml context
        MLContext mlContext = new MLContext(seed: 42);

        string dataPath = Path.Combine(Environment.CurrentDirectory, "data", "employee_attrition.csv");

        // load data into IDataView
        IDataView dataView = mlContext.Data.LoadFromTextFile<AttritionData>(dataPath, hasHeader: true, separatorChar: ',');

        // split data
        var trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // build pipeline for  Advanced Data Transformation Pipeline
        var dataPrepPipeline = mlContext.Transforms.Categorical.OneHotEncoding(new []{
                new InputOutputColumnPair("DepartmentEncoded", "Department"),
                new InputOutputColumnPair("JobRoleEncoded", "JobRole"),
                new InputOutputColumnPair("BusinessTravelEncoded", "BusinessTravel")
            })
            // Combine raw numeric items into a temporary vector for bulk scaling
            .Append(mlContext.Transforms.Concatenate("UnscaledNumerics",
                "Age", "MonthlyIncome", "YearsAtCompany", "YearsSincePromotion",
                "WorkLifeBalance", "JobSatisfaction", "OverTime", "DistanceFromHome",
                "NumCompaniesWorked", "TrainingTimesLastYear", "PerformanceRating"))
            // NEW CONCEPT: Normalize features uniformly to prevent high-value metrics from overpowering the weight calculations
            .Append(mlContext.Transforms.NormalizeMinMax("ScaledNumerics", "UnscaledNumerics"))
            // Build final feature vector using scaled variables and encoded string columns
            .Append(mlContext.Transforms.Concatenate("Features",
                "ScaledNumerics", "DepartmentEncoded", "JobRoleEncoded", "BusinessTravelEncoded"));



        // build model from pipeline
        // =========================================================================
        // PIPELINE 1: SDCA Logistic Regression (Stochastic Dual Coordinate Ascent)
        // =========================================================================
        Console.WriteLine("--- Training SDCA Linear Logistic Regression ---");
        var sdcaPipeline = dataPrepPipeline.Append(
            mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                new Microsoft.ML.Trainers.SdcaLogisticRegressionBinaryTrainer.Options
                {
                    L1Regularization = 0.05f,       // Controls sparsity (forces useless weights to exactly 0)
                    L2Regularization = 0.01f,       // Controls weight size (prevents extreme coefficients)
                    MaximumNumberOfIterations = 50  // Prevents over-optimizing on the train subset
                }));


        // trainModel
        var sdcaModel = sdcaPipeline.Fit(trainTestSplit.TrainSet);

        // evaluate model
        var sdcaTestResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, sdcaModel);
        var sdacaTrainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, sdcaModel);
        EvaluationEngine.PrintEvaluationMetrics(sdacaTrainResults.AlgorithmName, sdacaTrainResults, sdcaTestResults);
        Console.WriteLine($"SDCA Test F1-Score: {sdcaTestResults.Metrics["F1Score"]:F4}");

        // =========================================================================
        // PIPELINE 2: Averaged Perceptron (Error-driven Linear Binary Classifier)
        // =========================================================================
        Console.WriteLine("\n--- Training Averaged Perceptron ---");
        var perceptronPipeline = dataPrepPipeline.Append(
            mlContext.BinaryClassification.Trainers.AveragedPerceptron(
                new Microsoft.ML.Trainers.AveragedPerceptronTrainer.Options
                {
                    LearningRate = 0.05f,
                    DecreaseLearningRate = true, // Dampens updates as training progresses
                    NumberOfIterations = 20
                }))

                /* the AveragedPerceptron is a non-probabilistic linear classifier. 
                 * It outputs raw Score bounds, but does not output a Probability column by default. 
                 * When your loop runs Pipeline 2, 
                 * EvaluateBinary crashes because Probability is completely missing from the schema*/
                // ADD THIS LINE TO FIX PERCEPTRON PROBABILITIES:
                .Append(mlContext.BinaryClassification.Calibrators.Platt()); // 

        var perceptronModel = perceptronPipeline.Fit(trainTestSplit.TrainSet);
        var perceptronTestResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, perceptronModel);
        var perceptionTrainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, perceptronModel);
        EvaluationEngine.PrintEvaluationMetrics(perceptionTrainResults.AlgorithmName, perceptionTrainResults, perceptronTestResults);

        Console.WriteLine($"Perceptron Test F1-Score: {perceptronTestResults.Metrics["F1Score"]:F4}");

        // =========================================================================
        // APPROACH B: AutoML Hyperparameter Tuning 
        // =========================================================================
        uint trainingTime = 120;
        Console.WriteLine($"\n--- Starting AutoML Experiment ({trainingTime} Seconds) ---");

        var trainedDataPrepModel = dataPrepPipeline.Fit(trainTestSplit.TrainSet);
        var transformedTrainData = trainedDataPrepModel.Transform(trainTestSplit.TrainSet);
        var transformedTestData = trainedDataPrepModel.Transform(trainTestSplit.TestSet);

        var experimentSettings = new BinaryExperimentSettings
        {
            MaxExperimentTimeInSeconds = trainingTime,
            OptimizingMetric = BinaryClassificationMetric.F1Score,
        };

        // Filter out LightGBM/FastTree via custom trainer exclusion if you want to force AutoML to try others
        experimentSettings.Trainers.Remove(BinaryClassificationTrainer.LightGbm);
        experimentSettings.Trainers.Remove(BinaryClassificationTrainer.FastTree);

        var experiment = mlContext.Auto().CreateBinaryClassificationExperiment(experimentSettings);
        var automlRun = experiment.Execute(transformedTrainData, transformedTestData, labelColumnName: "Label");

        // Check if the AutoML model output schema contains "Probability"
        var automlPredictions = automlRun.BestRun.Model.Transform(transformedTestData);
        ITransformer finalAutoMlModel = automlRun.BestRun.Model;

        // If the winner did not create a Probability column, train a calibrator on it
        if (automlPredictions.Schema.GetColumnOrNull("Probability") == null)
        {
            Console.WriteLine($"-> Calibrating AutoML winner ({automlRun.BestRun.TrainerName})...");

            // Get scores from the training set to fit the calibrator accurately
            var scoredAutoMlTrain = finalAutoMlModel.Transform(transformedTrainData);

            var plattEstimator = mlContext.BinaryClassification.Calibrators.Platt();
            var plattTransformer = plattEstimator.Fit(scoredAutoMlTrain);

            finalAutoMlModel = finalAutoMlModel.Append(plattTransformer);
        }


        var automlModel = trainedDataPrepModel.Append(finalAutoMlModel);
        var automlTestResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TestSet, automlModel);
        var automlTrainResults = EvaluationEngine.EvaluateBinary(mlContext, trainTestSplit.TrainSet, automlModel);
        EvaluationEngine.PrintEvaluationMetrics(automlTrainResults.AlgorithmName, automlTrainResults, automlTestResults);

        Console.WriteLine($"AutoML Winner ({automlRun.BestRun.TrainerName}) F1-Score: {automlTestResults.Metrics["F1Score"]:F4}");

        // Evaluate the models
        // =========================================================================
        // CHAMPION EVALUATION & DEPLOYMENT
        // =========================================================================
        var candidateModels = new Dictionary<string, (ITransformer Model, double F1)>
        {
            { "SDCA", (sdcaModel, sdcaTestResults.Metrics["F1Score"]) },
            { "Perceptron", (perceptronModel, perceptronTestResults.Metrics["F1Score"]) },
            { "AutoML", (automlModel, automlTestResults.Metrics["F1Score"]) }
        };

        var champion = candidateModels.OrderByDescending(x => x.Value.F1).First();
        Console.WriteLine($"\nDeploying Champion Framework: {champion.Key} with F1: {champion.Value.F1:F4}");
        
        // save the best model

        string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\.."));
        string modelDeployPath = Path.Combine(projectRoot, "model", "EmployeeAttritionModel.zip");

        Directory.CreateDirectory(Path.GetDirectoryName(modelDeployPath)!);
        mlContext.Model.Save(champion.Value.Model, trainTestSplit.TrainSet.Schema, modelDeployPath);
        Console.WriteLine($"Saved configuration to source folder: {modelDeployPath}");

        

    }
}
