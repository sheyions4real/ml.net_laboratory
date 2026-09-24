using Microsoft.ML.Data;

namespace CustomerChurn.Training.Models;

public class ChurnData
{
    [LoadColumn(0)] public string CustomerId { get; set; } = string.Empty;
    [LoadColumn(1)] public float TenureMonths { get; set; }
    [LoadColumn(2)] public float MonthlyCharges { get; set; }
    [LoadColumn(3)] public float TotalCharges { get; set; }
    [LoadColumn(4)] public string ContractType { get; set; } = string.Empty;
    [LoadColumn(5)] public string PaymentMethod { get; set; } = string.Empty;
    [LoadColumn(6)] public string InternetService { get; set; } = string.Empty;
    [LoadColumn(7)] public string OnlineSecurity { get; set; } = string.Empty;
    [LoadColumn(8)] public string TechSupport { get; set; } = string.Empty;
    [LoadColumn(9)] public string StreamingTV { get; set; } = string.Empty;
    [LoadColumn(10)] public float SeniorCitizen { get; set; }
    [LoadColumn(11)] public float Partner { get; set; }
    [LoadColumn(12)] public float Dependents { get; set; }
    [LoadColumn(13)] public float NumSupportTickets { get; set; }
    [LoadColumn(14)] public string LastContactDate { get; set; } = string.Empty;
    [LoadColumn(15), ColumnName("Label")] public bool IsChurned { get; set; }

}

public class CustomerPrediction
{
    [ColumnName("PredictedLabel")] public bool Prediction { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
}

