
using MlAgentToolCli.Commands;
using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("ML-Agent Master Toolkit: A centralized inference gateway optimized for AI Agent workflows.");

        rootCommand.Subcommands.Add(CreditFraudCommand.Create());
        rootCommand.Subcommands.Add(CustomerChurnCommand.Create());
        rootCommand.Subcommands.Add(EmployeeAttritionCommand.Create());

        return await rootCommand.Parse(args).InvokeAsync();
    }
}