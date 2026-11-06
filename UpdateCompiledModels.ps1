$ErrorActionPreference = 'Stop'
$providers = @('MsSql', 'MySql')
$failed = @()

foreach ($provider in $providers) {
    $package = "Corely.IAM.CompiledModel.$provider"
    $generated = Join-Path $PSScriptRoot "$package/Generated"
    Write-Host "Regenerating the $provider compiled model in $package..." -ForegroundColor Cyan

    if (Test-Path $generated) {
        Remove-Item -Recurse -Force $generated
    }

    Push-Location (Join-Path $PSScriptRoot "Corely.IAM.DataAccessMigrations.$provider")
    try {
        dotnet ef dbcontext optimize --context IamDbContext --output-dir "../$package/Generated" --namespace "$package.Generated"
        if ($LASTEXITCODE -ne 0) {
            $failed += $provider
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host "Compiled models regenerated for: $($providers -join ', ')." -ForegroundColor Green
} else {
    Write-Host "Compiled model regeneration failed for: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
