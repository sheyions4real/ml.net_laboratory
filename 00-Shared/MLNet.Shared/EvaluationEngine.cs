using Microsoft.ML;
using Microsoft.ML.Data;
using MLNet.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

public static class EvaluationEngine
{
    // Overload for Binary Classification with threshold tuning support
    public static EvaluationResultDto EvaluateBinary(
        MLContext mlContext,
        IDataView testData,
        ITransformer model,
        string labelColumnName = "Label",
        string scoreColumnName = "Score",
        float threshold = 0.50f) // <-- Added threshold argument defaulting to 0.50
    {
        // 1. Generate predictions from the model
        var predictions = model.Transform(testData);

        // 2. Compute the standard metrics (Accuracy, AUC, LogLoss don't care about threshold boundaries)
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName, scoreColumnName);

        // 3. Extract columns into raw arrays to manually compute the F1-Score at your custom threshold
        var probabilities = predictions.GetColumn<float>("Probability").ToArray();
        var labels = predictions.GetColumn<bool>(labelColumnName).ToArray();

        int truePositives = 0;
        int falsePositives = 0;
        int falseNegatives = 0;

        for (int i = 0; i < probabilities.Length; i++)
        {
            // Evaluate prediction using our custom threshold instead of 0.50
            bool predictedTrue = probabilities[i] >= threshold;
            bool actualTrue = labels[i];

            if (predictedTrue && actualTrue) truePositives++;
            if (predictedTrue && !actualTrue) falsePositives++;
            if (!predictedTrue && actualTrue) falseNegatives++;
        }

        // Standard machine learning equations for Precision, Recall, and F1-Score
        double precision = (truePositives + falsePositives) > 0 ? (double)truePositives / (truePositives + falsePositives) : 0;
        double recall = (truePositives + falseNegatives) > 0 ? (double)truePositives / (truePositives + falseNegatives) : 0;
        double customF1Score = (precision + recall) > 0 ? (2 * precision * recall) / (precision + recall) : 0;

        // 4. Calculate an optimized accuracy for that threshold boundary
        int totalRows = probabilities.Length;
        int trueNegatives = totalRows - (truePositives + falsePositives + falseNegatives);
        double customAccuracy = (double)(truePositives + trueNegatives) / totalRows;

        return new EvaluationResultDto
        {
            AlgorithmName = model.ToString() ?? "Binary Classifier",
            Metrics = new Dictionary<string, double>
            {
                // Accuracy and F1-Score now accurately represent your custom boundary threshold
                {"Accuracy", customAccuracy },
                {"AreaUnserRocCurve", metrics.AreaUnderRocCurve },
                {"F1Score", customF1Score },
                {"LogLoss", metrics.LogLoss },
                // --- NEW EXTENDED METRICS ---
                { "Precision", precision },
                { "Recall", recall },
                { "TruePositives", truePositives },
                { "FalsePositives", falsePositives },
                { "FalseNegatives", falseNegatives },
                { "TrueNegatives", trueNegatives }
            }
        };
    }


    public static void PrintEvaluationMetrics(string algorithmName, EvaluationResultDto trainResult, EvaluationResultDto testResult)
    {
        // 3. Print side-by-side comparison
        Console.WriteLine($"\n--- Metric Comparison ({algorithmName}) ---");
        Console.WriteLine($"Metric                 | Train Set | Test Set");
        Console.WriteLine($"-----------------------------------------");

        foreach (var metric in testResult.Metrics)
        {
            double trainValue = trainResult.Metrics.ContainsKey(metric.Key) ? trainResult.Metrics[metric.Key] : 0.0;
            Console.WriteLine($"{metric.Key,-22} | {trainValue:F4}    | {metric.Value:F4}");
        }
    }
}
