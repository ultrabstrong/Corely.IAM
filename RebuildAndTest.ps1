csharpier format .
dotnet clean Corely.IAM.sln --verbosity minimal
dotnet build Corely.IAM.sln --verbosity minimal
dotnet test --collect:"XPlat Code Coverage"