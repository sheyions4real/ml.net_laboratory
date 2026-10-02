using MlAgentToolCli.Infrastructure;
using MLNet.Shared.Dtos;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MlAgentToolCli.Commands;

public static class CreditFraudCommand
{
    public static Command Create()
    {
        var command = new Command("credit-fraud",
            "Assess immediate transaction settlement fraud risks.\n\n" +
            "💡 AGENT EXAMPLE PAYLOAD:\n" +
            "  ml-agent credit-fraud -TransactionAmount 2090.98 -DistanceFromHome 98.7");

        var amountOpt = new Option<float>("-TransactionAmount")
        { 
            Description = "The dollar valuation size of the transaction charge."
        };
        var distanceOpt = new Option<float>("-DistanceFromHome")
        {
            Description = "Physical distance calculation away from the home address profile."
        };


        command.Options.Add(amountOpt);
        command.Options.Add(distanceOpt);

        command.SetAction(parseResult =>
        {
            try
            {
                var engine = ModelEngineFactory.CreateEngine<TransactionData, TransactionPrediction>("fraud");

                var input = new TransactionData
                {
                    TransactionAmount = parseResult.GetValue(amountOpt),
                    DistanceFromHome = parseResult.GetValue(distanceOpt),
                    MerchantCategory = "Restaurant",
                    DeviceType = "Mobile",
                    TransactionHour = 8
                };

                var prediction = engine.Predict(input);

                JsonGateway.RenderSuccess(new
                {
                    IsFraudulent = prediction.Prediction,
                    RiskProbability = prediction.Probability
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
