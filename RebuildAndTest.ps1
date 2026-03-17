csharpier format .
dotnet clean Corely.IAM.slnx --verbosity minimal
dotnet build Corely.IAM.slnx --verbosity minimal
dotnet publish Corely.IAM.DataAccessMigrations.Cli\Corely.IAM.DataAccessMigrations.Cli.csproj -c Release -r win-x64 -p:DebugType=none
dotnet publish Corely.IAM.DevTools\Corely.IAM.DevTools.csproj -c Release -r win-x64 -p:DebugType=none
dotnet test --collect:"XPlat Code Coverage"