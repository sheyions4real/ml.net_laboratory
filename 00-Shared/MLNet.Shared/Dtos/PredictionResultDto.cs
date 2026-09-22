using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLNet.Shared.Dtos;

public class PredictionResultDto<TResult>
{
    public required TResult Prediction { get; set; }
    public float Confidence { get; set; }
}

public class EvaluationResultDto
{
    public string AlgorithmName { get; set; } = string.Empty;
    public Dictionary<string, double> Metrics { get; set; } = new();
}
