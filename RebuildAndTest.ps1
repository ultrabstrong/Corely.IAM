$env:MSBUILDDISABLENODEREUSE = '1'
dotnet tool restore
dotnet csharpier format .
dotnet clean Corely.IAM.slnx --verbosity minimal
dotnet build Corely.IAM.slnx --verbosity minimal
dotnet publish Corely.IAM.DataAccessMigrations.Cli\Corely.IAM.DataAccessMigrations.Cli.csproj -c Release -r win-x64 -p:DebugType=none
dotnet publish Corely.IAM.DevTools\Corely.IAM.DevTools.csproj -c Release -r win-x64 -p:DebugType=none
dotnet build-server shutdown | Out-Null

function Wait-FreeMemory([double]$MinimumGB = 2) {
    if (-not $IsWindows) { return }
    for ($i = 0; $i -lt 60; $i++) {
        $freeGB = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1MB
        if ($freeGB -ge $MinimumGB) { return }
        if ($i -eq 0) { Write-Host ("Waiting for {0} GB free memory, {1:N1} GB now" -f $MinimumGB, $freeGB) }
        Start-Sleep -Seconds 10
    }
    Write-Warning "Starting with less than $MinimumGB GB free memory"
}

$projects = dotnet sln Corely.IAM.slnx list |
    Where-Object { $_ -match 'Tests\.csproj$' } |
    Sort-Object { $_ -match 'IntegrationTests|FunctionalTests' }, { $_ }

$failed = @()
foreach ($project in $projects) {
    Wait-FreeMemory
    dotnet test --project $project --no-build --coverage
    if ($LASTEXITCODE -ne 0) { $failed += $project }
}

if ($failed) {
    Write-Host "Failed: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
