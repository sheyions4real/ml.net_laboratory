using System.IO;
using System.Threading.Tasks;
using System.CommandLine;
using Xunit;
using MlAgentToolCli.Commands;

namespace MlAgentToolCli.Tests
{
    public class CliIntegrationTests
    {
        private readonly RootCommand _rootCommand;

        public CliIntegrationTests()
        {
            // Set up a mock root orchestrator matching Program.Main structure
            _rootCommand = new RootCommand("Test Root Router");

            _rootCommand.Subcommands.Add(CustomerChurnCommand.Create());
            _rootCommand.Subcommands.Add(EmployeeAttritionCommand.Create());
            _rootCommand.Subcommands.Add(CreditFraudCommand.Create());
        }

        [Fact]
        public async Task Root_HelpFlag_ReturnsSuccessExitCode()
        {
            // Arrange
            string[] args = new[] { "--help" };

            // Act
            int exitCode = await _rootCommand.Parse(args).InvokeAsync();

            // Assert
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public async Task CustomerChurn_HelpFlag_IncludesAgentExamplePayload()
        {
            // Arrange
            string[] args = new[] { "customer-churn", "--help" };
            using var consoleWriter = new StringWriter();

            // Redirect System.CommandLine output away from standard out to intercept text strings
            var originalOut = System.Console.Out;
            System.Console.SetOut(consoleWriter);

            try
            {
                // Act
                int exitCode = await _rootCommand.Parse(args).InvokeAsync();
                string outputText = consoleWriter.ToString();

                // Assert
                Assert.Equal(0, exitCode);
                Assert.Contains("AGENT EXAMPLE PAYLOAD", outputText);
                Assert.Contains("-TenureMonths", outputText);
            }
            finally
            {
                System.Console.SetOut(originalOut); // Revert console hooks safely
            }
        }

        [Fact]
        public async Task CustomerChurn_ValidArguments_InvokesHandlerLoop()
        {
            // Arrange
            string[] args = new[] {
                "customer-churn",
                "-TenureMonths", "26",
                "-MonthlyCharges", "61.83",
                "-NumSupportTickets", "12"
            };

            using var consoleWriter = new StringWriter();
            var originalOut = System.Console.Out;
            System.Console.SetOut(consoleWriter);

            try
            {
                // Act
                int exitCode = await _rootCommand.Parse(args).InvokeAsync();
                string outputText = consoleWriter.ToString();

                // Assert
                // If model zip files are missing during testing, ensure it falls through to the JSON error payload
                if (exitCode != 0)
                {
                    Assert.Contains("MODEL_NOT_DEPLOYED", outputText);
                    Assert.Contains("Error", outputText);
                }
                else
                {
                    Assert.Contains("Success", outputText);
                    Assert.Contains("isChurned", outputText);
                }
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
        }
    }
}