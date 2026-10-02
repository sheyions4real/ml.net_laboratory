using Microsoft.ML;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MlAgentToolCli.Infrastructure;

public static class ModelEngineFactory
{
    private static readonly MLContext mlContext = new MLContext(seed: 42);
    private static readonly ConcurrentDictionary<string, ITransformer> ModelCache = new();

    public static PredictionEngine<TInput, TOutput> CreateEngine<TInput, TOutput>(string modelName)
        where TInput: class
        where TOutput: class, new()
    {
        string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MlModels", modelPathMapping(modelName));
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"Model footprint missing for key '{modelName}' at path: {modelPath}");
        }

        // Fetch cached binary layout or load fresh
        var trainedModel = ModelCache.GetOrAdd(modelName, (_) =>
        {
            // use stream to load the model
            //using var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            //return mlContext.Model.Load(stream, out DataViewSchema _);

            // or
            // FIX: Explicitly type the out variable as DataViewSchema
            return mlContext.Model.Load(modelPath, out DataViewSchema _);
        });

        return mlContext.Model.CreatePredictionEngine<TInput, TOutput>(trainedModel);
    }

    private static string modelPathMapping(string name) => name.ToLowerInvariant() switch
    {
        "fraud" => "FraudModel.zip",
        "churn" => "CustomerChurnModel.zip",
        "attrition" => "EmployeeAttritionModel.zip",
        _ => throw new ArgumentException($"Invalid internal profile key mapping: {name}")
    };
}
