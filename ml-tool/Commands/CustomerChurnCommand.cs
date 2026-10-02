using MlAgentToolCli.Infrastructure;
using MLNet.Shared.Dtos;
using System.CommandLine;

namespace MlAgentToolCli.Commands
{
    public static class CustomerChurnCommand
    {
        public static Command Create()
        {
            // define the command
            var command = new Command("customer-churn",
                "Evaluate consumer churn probability markers using historical support metrics.\n\n" +
                "💡 AGENT EXAMPLE PAYLOAD:\n" +
                "  ml-agent customer-churn -TenureMonths 26 -MonthlyCharges 61.83 -NumSupportTickets 12");

            // create the params
            var tenureOpt = new Option<float>("-TenureMonths")
            {
                Description = "The number of months the customer has stayed with the company."
            }; 
            
            var monthlyOpt = new Option<float>("-MonthlyCharges")
            {
                Description = "The amount charged to the customer monthly."
            };

            var ticketsOpt = new Option<float>("-NumSupportTickets")
            {
                Description = "Number of support tickets opened in the last cycle."
            };

            // add the params to command
            command.Options.Add(tenureOpt);
            command.Options.Add(monthlyOpt);
            command.Options.Add(ticketsOpt);

            // define the handler for this command
            command.SetAction(parseResult =>
            {
                try
                {
                    var engine = ModelEngineFactory.CreateEngine<ChurnData, CustomerPrediction>("churn");

                    var input = new ChurnData
                    {
                        TenureMonths = parseResult.GetValue(tenureOpt),
                        MonthlyCharges = parseResult.GetValue(monthlyOpt),
                        NumSupportTickets = parseResult.GetValue(ticketsOpt),
                        ContractType = "Two year",
                        PaymentMethod = "Bank transfer",
                        InternetService = "Fiber optic"
                    };

                    var prediction = engine.Predict(input);

                    JsonGateway.RenderSuccess(new
                    {
                        IsChurned = prediction.Prediction,
                        Confidence = prediction.Probability
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
}
