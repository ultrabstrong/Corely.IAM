[CmdletBinding()]
param(
    [switch]$Reset,
    [switch]$NoSeed,
    [switch]$NoRun
)

$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) {
    throw 'Run this with PowerShell 7 (pwsh), not Windows PowerShell.'
}
$ProgressPreference = 'SilentlyContinue'

$repoRoot = $PSScriptRoot
$webAppDir = Join-Path $repoRoot 'Corely.IAM.WebApp'
$settingsPath = Join-Path $webAppDir 'appsettings.json'
$templatePath = Join-Path $webAppDir 'appsettings.template.json'
$cliProject = Join-Path $repoRoot 'Corely.IAM.DataAccessMigrations.Cli'
$devToolsProject = Join-Path $repoRoot 'Corely.IAM.DevTools'
$devToolsBin = Join-Path $devToolsProject 'bin' 'Debug' 'net10.0'
$seedScript = Join-Path $webAppDir 'DemoSetup' 'SeedWebAppDemo.ps1'
$appUrl = 'https://localhost:7100'
$appPort = 7100
$legacyHistoryTable = '__EFMigrationsHistory'

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-Checked([string]$What, [scriptblock]$Command) {
    $output = & $Command 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$What failed:`n$($output -join "`n")"
    }
    return $output
}

function Test-PortListening([int]$Port) {
    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        return $client.ConnectAsync('127.0.0.1', $Port).Wait(250)
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function New-SystemKey {
    $bytes = New-Object byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    return [Convert]::ToBase64String($bytes)
}

if (-not (Test-Path $settingsPath)) {
    Write-Step 'Creating Corely.IAM.WebApp/appsettings.json for LocalDB'
    $settings = Get-Content $templatePath -Raw | ConvertFrom-Json
    $settings.ConnectionStrings.DefaultConnection = 'Server=(localdb)\MSSQLLocalDB;Database=CorelyIAM;Trusted_Connection=True;TrustServerCertificate=True;'
    $settings.Security.SystemKey = New-SystemKey
    $settings | ConvertTo-Json -Depth 20 | Set-Content $settingsPath
}

$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$connection = $settings.ConnectionStrings.DefaultConnection
$provider = if ("$($settings.Database.Provider)" -ieq 'mysql') { 'MySql' } else { 'MsSql' }
if ([string]::IsNullOrWhiteSpace($connection) -or $connection -like '<*') {
    throw "ConnectionStrings:DefaultConnection is not set in $settingsPath"
}

if ($connection -match '\(localdb\)\\([^;]+)') {
    $instance = $Matches[1]
    Write-Step "Starting LocalDB instance $instance"
    if (Get-Command sqllocaldb -ErrorAction SilentlyContinue) {
        sqllocaldb start $instance | Out-Null
    }
    else {
        Write-Warning 'sqllocaldb was not found; assuming the instance is already running.'
    }
}

if (Test-PortListening $appPort) {
    throw "Port $appPort is already in use; a WebApp is probably still running, and it would also lock the files the build needs. Stop it (Stop-Process -Name Corely.IAM.WebApp) and run this again."
}

Write-Step 'Building the migration tool, DevTools and the WebApp'
foreach ($project in $cliProject, $devToolsProject, $webAppDir) {
    Invoke-Checked "Building $(Split-Path $project -Leaf)" { dotnet build $project -v q -nologo } | Out-Null
}

function Invoke-Db([string[]]$Arguments) {
    $output = dotnet run --project $cliProject --no-build -- db @Arguments -p $provider -c $connection 2>&1
    return ($output | ForEach-Object { "$_" })
}

if ($Reset) {
    Write-Step 'Dropping the database (-Reset)'
    Invoke-Db @('drop', '--force') | Out-Null
}

$historyArguments = @()
$fresh = $false
$canConnect = (Invoke-Db @('test-connection')) -match 'Successfully connected'

if (-not $canConnect) {
    Write-Step 'Creating the database and applying every migration'
    $output = Invoke-Db @('create')
    if (-not ($output -match 'Database created and migrations applied successfully')) {
        throw "Creating the database failed:`n$($output -join "`n")"
    }
    $fresh = $true
}
else {
    $current = Invoke-Db @('list')
    if (-not ($current -match '\[Applied\]')) {
        $legacy = Invoke-Db @('list', '--history-table', $legacyHistoryTable)
        if ($legacy -match '\[Applied\]') {
            Write-Host "    Migration history found in $legacyHistoryTable; using it."
            $historyArguments = @('--history-table', $legacyHistoryTable)
        }
        else {
            $fresh = $true
        }
    }

    Write-Step 'Applying pending migrations'
    $output = Invoke-Db (@('migrate') + $historyArguments)
    if (-not ($output -match 'All migrations applied successfully')) {
        throw "Migrating the database failed:`n$($output -join "`n")"
    }
}

if ($NoSeed) {
    Write-Step 'Skipping the demo seed (-NoSeed)'
}
elseif (-not $fresh) {
    Write-Step 'Database already had data; leaving it as it is (run with -Reset for a clean, seeded one)'
}
else {
    Write-Step 'Seeding demo users and accounts (takes a few minutes)'
    @{
        Provider = $provider
        ConnectionStrings = @{ DataRepoConnection = $connection }
        SystemSymmetricEncryptionKey = $settings.Security.SystemKey
        PasswordValidationOptions = $settings.PasswordValidationOptions
    } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $devToolsBin 'corely-iam-devtool-settings.json')

    $devToolsDll = Join-Path $devToolsBin 'Corely.IAM.DevTools.dll'
    Set-Item -Path function:global:corely -Value { & dotnet $devToolsDll @args }.GetNewClosure()
    try {
        & $seedScript -AuthTokenFile (Join-Path $devToolsBin 'corely-iam-auth-token.json')
    }
    finally {
        Remove-Item function:global:corely -ErrorAction SilentlyContinue
    }
}

Write-Host ''
Write-Host 'Local IAM is ready.' -ForegroundColor Green
Write-Host "  Open:     $appUrl"
Write-Host '  Sign in:  admin / Test1234 (seeded demo users all use Test1234)'
if ($NoRun) {
    Write-Host "  Run it:   dotnet run --project Corely.IAM.WebApp (or run this script without -NoRun)"
    return
}

Write-Step "Starting the WebApp on $appUrl (Ctrl+C stops it)"
$app = Start-Process dotnet -NoNewWindow -PassThru -WorkingDirectory $webAppDir -ArgumentList @(
    'run', '--no-build', '--launch-profile', 'Corely.IAM.WebApp', '--',
    '--Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime=Information'
)
try {
    $deadline = (Get-Date).AddMinutes(2)
    while (-not (Test-PortListening $appPort)) {
        if ($app.HasExited) {
            throw "The WebApp exited during startup with code $($app.ExitCode)."
        }
        if ((Get-Date) -gt $deadline) {
            throw "The WebApp did not start listening on $appPort within two minutes."
        }
        Start-Sleep -Milliseconds 500
    }
    try {
        Start-Process $appUrl
    }
    catch {
        Write-Warning "Could not open a browser; go to $appUrl yourself."
    }
    $app.WaitForExit()
}
finally {
    if (-not $app.HasExited) {
        $app.Kill($true)
    }
}
