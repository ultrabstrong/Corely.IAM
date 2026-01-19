# AddMigration.ps1
# Creates a migration in all provider projects
#
# Usage: .\AddMigration.ps1 <MigrationName>
# Example: .\AddMigration.ps1 AddUserPreferences

param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb",
    "Corely.IAM.DataAccessMigrations.MsSql"
)

$failed = @()

foreach ($project in $projects) {
    Write-Host "`nCreating migration '$MigrationName' in $project..." -ForegroundColor Cyan
    
    if (-not (Test-Path $project)) {
        Write-Warning "Project directory not found: $project"
        $failed += $project
        continue
    }
    
    Push-Location $project
    try {
        dotnet ef migrations add $MigrationName
        if ($LASTEXITCODE -ne 0) {
            $failed += $project
        }
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host "Migration '$MigrationName' created in all projects." -ForegroundColor Green
} else {
    Write-Host "Migration '$MigrationName' failed in: $($failed -join ', ')" -ForegroundColor Red
}
