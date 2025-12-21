csharpier format .
dotnet clean Corely.IAM.sln --verbosity minimal
dotnet build Corely.IAM.sln --verbosity minimal
dotnet publish Corely.IAM.DataAccessMigrations\Corely.IAM.DataAccessMigrations.csproj -c Release -r win-x64
dotnet test --collect:"XPlat Code Coverage"