using MlAgentToolCli.Infrastructure;
using MLNet.Shared.Dtos;
using System.CommandLine;

namespace MlAgentToolCli.Commands;

public static class EmployeeAttritionCommand
{
    public static Command Create()
    {
        var command = new Command("employee-attrition",
            "Predict structural workforce attrition hazards.\n\n" +
            "💡 AGENT EXAMPLE PAYLOAD:\n" +
            "  ml-agent employee-attrition -Age 56 -MonthlyIncome 7118 -YearsAtCompany 14.6 -OverTime 0");

        var ageOpt = new Option<float>("-Age") { Description = "Age of the target employee footprint." };
        var incomeOpt = new Option<float>("-MonthlyIncome") { Description = "Monthly structural paycheck compensation in USD." };
        var yearsOpt = new Option<float>("-YearsAtCompany") { Description = "Total years tracked within the corporate organization." };
        var otOpt = new Option<float>("-OverTime") { Description = "Is employee working overtime hours? (1 = Yes, 0 = No)" };

        command.Options.Add(ageOpt);
        command.Options.Add(incomeOpt);
        command.Options.Add(yearsOpt);
        command.Options.Add(otOpt);

        command.SetAction(parseResult =>
        {
            try
            {
                var engine = ModelEngineFactory.CreateEngine<AttritionData, AttritionPrediction>("attrition");

                var input = new AttritionData
                {
                    Age = parseResult.GetValue(ageOpt),
                    MonthlyIncome = parseResult.GetValue(incomeOpt),
                    YearsAtCompany = parseResult.GetValue(yearsOpt),
                    OverTime = parseResult.GetValue(otOpt),
                    Department = "Finance",
                    JobRole = "Representative",
                    BusinessTravel = "Non-Travel"
                };

                var prediction = engine.Predict(input);

                JsonGateway.RenderSuccess(new
                {
                    HasLeft = prediction.Prediction,
                    RiskProbability = prediction.Probability,
                    RawScore = prediction.Score
                });
            }
            catch (FileNotFoundException ex)
            {
                JsonGateway.RenderError("MODEL_NOT_DEPLOYED", ex.Message);
            }
        });

        return command;
    }
}