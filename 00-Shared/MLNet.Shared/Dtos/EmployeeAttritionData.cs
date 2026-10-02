using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLNet.Shared.Dtos;

public class AttritionData
{
    [LoadColumn(0)] public string EmployeeId { get; set; } = string.Empty;
    [LoadColumn(1)] public float Age { get; set; }
    [LoadColumn(2)] public string Department { get; set; } = string.Empty;
    [LoadColumn(3)] public string JobRole { get; set; } = string.Empty;
    [LoadColumn(4)] public float MonthlyIncome { get; set; }
    [LoadColumn(5)] public float YearsAtCompany { get; set; }
    [LoadColumn(6)] public float YearsSincePromotion { get; set; }
    [LoadColumn(7)] public float WorkLifeBalance { get; set; }
    [LoadColumn(8)] public float JobSatisfaction { get; set; }
    [LoadColumn(9)] public float OverTime { get; set; } // 0 or 1
    [LoadColumn(10)] public string BusinessTravel { get; set; } = string.Empty;
    [LoadColumn(11)] public float DistanceFromHome { get; set; }
    [LoadColumn(12)] public float NumCompaniesWorked { get; set; }
    [LoadColumn(13)] public float TrainingTimesLastYear { get; set; }
    [LoadColumn(14)] public float PerformanceRating { get; set; }
    [LoadColumn(15), ColumnName("Label")] public bool HasLeft { get; set; }
}


public class AttritionPrediction
{
    [ColumnName("PredictedLabel")] public bool Prediction { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
}
