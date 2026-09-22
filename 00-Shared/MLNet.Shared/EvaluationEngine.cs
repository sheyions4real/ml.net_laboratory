using Microsoft.ML;
using MLNet.Shared.Dtos;

namespace MLNet.Shared
{
    public static class EvaluationEngine
    {
        // Overload for Binary Classification
        public static EvaluationResultDto EvaluateBinary(
            MLContext mlContext,
            IDataView testData,
            ITransformer model,
            string labelColumnName = "Label",
            string scoreColumnName = "Score")
        {
            var predictions = model.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName, scoreColumnName);

            return new EvaluationResultDto
            {
                AlgorithmName = model.ToString() ?? "Binary Classifier",
                Metrics = new Dictionary<string, double>
                {
                    {"Accuracy", metrics.Accuracy },
                    {"AreaUnserRocCurve", metrics.AreaUnderRocCurve },
                    {"F1Score", metrics.F1Score },
                    {"LogLoss", metrics.LogLoss },
                }
            };
        }


        // overload for multiclassification

        // overload for regression

        // overload for clustering
    }
}
