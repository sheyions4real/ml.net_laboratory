# 1. Create the main solution
dotnet new sln -n MLNetMasterSuite

# 2. Create the Shared Evaluation Library
dotnet new classlib -n MLNet.Shared

# 3. Create the Credit Card Fraud Detection ecosystem (Binary Classification)
# Create directories for models and data manually, then create projects:
dotnet new console -n CreditCardFraudDetection.Training
dotnet new webapi -n CreditCardFraudDetection.Api
dotnet new xunit -n CreditCardFraudDetection.Tests

# 4. Add projects to the main solution
dotnet sln add MLNet.Shared
dotnet sln add CreditCardFraudDetection.Training
dotnet sln add CreditCardFraudDetection.Api
dotnet sln add CreditCardFraudDetection.Tests

# 5. Add references
dotnet add CreditCardFraudDetection.Training reference MLNet.Shared
dotnet add CreditCardFraudDetection.Api reference MLNet.Shared
dotnet add CreditCardFraudDetection.Tests reference MLNet.Shared
dotnet add CreditCardFraudDetection.Tests reference CreditCardFraudDetection.Training
