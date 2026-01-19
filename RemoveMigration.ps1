# RemoveMigration.ps1
# Removes the last migration from all provider projects
#
# Usage: .\RemoveMigration.ps1
#
# NOTE: This script removes migration FILES only. It does not revert migrations
# that have been applied to a database. To revert applied migrations, use:
#   corely-db db migrate <previous-migration-name>

$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb",
    "Corely.IAM.DataAccessMigrations.MsSql"
)

$failed = @()

Write-Host ""
Write-Warning "This removes migration files only. To revert applied migrations, use 'corely-db db migrate <target>'."
Write-Host ""

foreach ($project in $projects) {
    Write-Host "Removing last migration from $project..." -ForegroundColor Cyan
    
    if (-not (Test-Path $project)) {
        Write-Warning "Project directory not found: $project"
        $failed += $project
        continue
    }
    
    Push-Location $project
    try {
        dotnet ef migrations remove --force
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
    Write-Host "Last migration removed from all projects." -ForegroundColor Green
} else {
    Write-Host "Migration removal failed in: $($failed -join ', ')" -ForegroundColor Red
}
