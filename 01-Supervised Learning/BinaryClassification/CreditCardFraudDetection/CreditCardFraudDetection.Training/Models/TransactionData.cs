using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreditCardFraudDetection.Training.Models;

public class TransactionData
{
    [LoadColumn(0)] public string TransactionId { get; set; } = string.Empty;
    [LoadColumn(1)] public string CardId { get; set; } = string.Empty;

    [LoadColumn(2)] public float TransactionAmount { get; set; }
    [LoadColumn(3)] public string MerchantCategory { get; set; } = string.Empty;
    [LoadColumn(4)] public float TransactionHour { get; set; }
    [LoadColumn(5)] public float DistanceFromHome { get; set; }
    [LoadColumn(6)] public float IsOnline { get; set; }
    [LoadColumn(7)] public float IsInternational { get; set; }
    [LoadColumn(8)] public float PreviousFraudFlag { get; set; }
    [LoadColumn(9)] public float AvgTransactionAmount30d { get; set; }
    [LoadColumn(10)] public float TransactionCount24h { get; set; }
    [LoadColumn(11)] public string DeviceType { get; set; } = string.Empty;
    [LoadColumn(12), ColumnName("Label")] public bool IsFraud { get; set; }
}


public class TransactionPrediction
{
    [ColumnName("PredictedLabel")] public bool Prediction { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}
