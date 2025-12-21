# Copy Corely tools to user profile directory
$targetDir = Join-Path $env:USERPROFILE "Corely"

# Create target directory if it doesn't exist
if (-not (Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    Write-Host "Created directory: $targetDir"
}

# Source paths for published executables
$devToolsSource = "Corely.IAM.DevTools\bin\Release\net8.0\win-x64\publish\Corely.IAM.DevTools.exe"
$dataAccessSource = "Corely.IAM.DataAccessMigrations\bin\Release\net8.0\win-x64\publish\Corely.IAM.DataAccessMigrations.exe"

# Copy and rename DevTools to corely.exe
if (Test-Path $devToolsSource) {
    Copy-Item $devToolsSource -Destination (Join-Path $targetDir "corely.exe") -Force
    Write-Host "Copied Corely.IAM.DevTools.exe to $targetDir\corely.exe"
} else {
  Write-Warning "DevTools executable not found at: $devToolsSource"
 Write-Warning "Run 'dotnet publish Corely.IAM.DevTools\Corely.IAM.DevTools.csproj -c Release -r win-x64' first"
}

# Copy and rename DataAccessMigrations to corely-db.exe
if (Test-Path $dataAccessSource) {
    Copy-Item $dataAccessSource -Destination (Join-Path $targetDir "corely-db.exe") -Force
    Write-Host "Copied Corely.IAM.DataAccessMigrations.exe to $targetDir\corely-db.exe"
} else {
    Write-Warning "DataAccessMigrations executable not found at: $dataAccessSource"
  Write-Warning "Run 'dotnet publish Corely.IAM.DataAccessMigrations\Corely.IAM.DataAccessMigrations.csproj -c Release -r win-x64' first"
}

# Check if directory is already in PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -split ";" -contains $targetDir) {
    Write-Host ""
    Write-Host "Directory already in PATH: $targetDir"
} else {
    # Add to user PATH
    $newPath = $currentPath + ";" + $targetDir
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    Write-Host ""
    Write-Host "Added $targetDir to user PATH"
    Write-Host "Restart your terminal for PATH changes to take effect."
}

Write-Host ""
Write-Host "Tools available:"
Write-Host "  corely     - DevTools (encryption, auth, registration commands)"
Write-Host "  corely-db  - Database migrations"
