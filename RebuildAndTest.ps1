csharpier format .
dotnet clean Corely.IAM.sln --verbosity minimal
dotnet build Corely.IAM.sln --verbosity minimal
dotnet publish Corely.IAM.DataAccessMigrations\Corely.IAM.DataAccessMigrations.csproj -c Release -r win-x64 -p:DebugType=none
dotnet publish Corely.IAM.DevTools\Corely.IAM.DevTools.csproj -c Release -r win-x64 -p:DebugType=none
dotnet test --collect:"XPlat Code Coverage"