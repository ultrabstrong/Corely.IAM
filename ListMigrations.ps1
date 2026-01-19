# ListMigrations.ps1
# Lists migrations in all provider projects
#
# Usage: .\ListMigrations.ps1

$projects = @(
    "Corely.IAM.DataAccessMigrations.MySql",
    "Corely.IAM.DataAccessMigrations.MariaDb"
)

foreach ($project in $projects) {
    Write-Host "`n=== $project ===" -ForegroundColor Cyan
    
    if (-not (Test-Path $project)) {
        Write-Warning "Project directory not found: $project"
        continue
    }
    
    Push-Location $project
    try {
        dotnet ef migrations list --no-connect
    }
    finally {
        Pop-Location
    }
}

Write-Host ""
