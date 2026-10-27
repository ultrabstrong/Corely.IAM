[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Package,
    [Parameter(Mandatory)][string]$Consumer,
    [switch]$Revert
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression

$repoRoot = Split-Path $PSScriptRoot -Parent
$feed = Join-Path ([System.IO.Path]::GetTempPath()) 'corely-local-feed'
$marker = Join-Path $feed "$Package.$((Split-Path $Consumer -Leaf)).original"
New-Item -ItemType Directory -Force $feed | Out-Null

function Get-CsprojVersion([string]$PackageId) {
    $csproj = Get-ChildItem $repoRoot -Recurse -Filter "$PackageId.csproj" |
        Where-Object FullName -notmatch '\\(bin|obj)\\' | Select-Object -First 1
    if (-not $csproj) { throw "No $PackageId.csproj in $repoRoot" }
    [pscustomobject]@{
        Path = $csproj.FullName
        Version = ([xml](Get-Content $csproj.FullName -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    }
}

function Get-ConsumerFiles {
    Get-ChildItem $Consumer -Recurse -Include Directory.Packages.props, *.csproj |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules)\\' } |
        Where-Object { (Get-Content $_.FullName -Raw) -match "Include=`"$([regex]::Escape($Package))`" Version=`"" }
}

function Set-ConsumerVersion([string]$Version) {
    foreach ($file in Get-ConsumerFiles) {
        $text = Get-Content $file.FullName -Raw
        $updated = $text -replace "(Include=`"$([regex]::Escape($Package))`" Version=`")[^`"]+", "`${1}$Version"
        if ($updated -ne $text) {
            Set-Content $file.FullName $updated -NoNewline
            "  $($file.FullName.Substring($Consumer.Length + 1)): $Package $Version"
        }
    }
}

function Get-ConsumerSolution {
    Get-ChildItem $Consumer -File | Where-Object Extension -in '.slnx', '.sln' | Select-Object -First 1
}

if ($Revert) {
    if (-not (Test-Path $marker)) { throw "No local build of $Package is recorded for $Consumer" }
    $original = (Get-Content $marker -Raw).Trim()
    Set-ConsumerVersion $original
    Remove-Item $marker
    dotnet restore (Get-ConsumerSolution).FullName --no-http-cache | Out-Null
    "Reverted $Package to $original in $Consumer"
    return
}

$project = Get-CsprojVersion $Package
$local = "$($project.Version)-local.$(Get-Date -Format 'yyyyMMddHHmmss')"

dotnet pack $project.Path -c Release -o $feed "-p:PackageVersion=$local" --force -v q -nologo
if ($LASTEXITCODE -ne 0) { throw "Packing $Package failed" }

$nupkg = Join-Path $feed "$Package.$local.nupkg"
$zip = [System.IO.Compression.ZipFile]::Open($nupkg, 'Update')
try {
    $entry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
    $reader = [System.IO.StreamReader]::new($entry.Open())
    $nuspec = $reader.ReadToEnd()
    $reader.Dispose()
    $fixed = [regex]::Replace($nuspec, "<dependency id=`"([^`"]+)`" version=`"$([regex]::Escape($local))`"", {
            param($m)
            "<dependency id=`"$($m.Groups[1].Value)`" version=`"$((Get-CsprojVersion $m.Groups[1].Value).Version)`""
        })
    $entry.Delete()
    $writer = [System.IO.StreamWriter]::new($zip.CreateEntry($entry.FullName).Open())
    $writer.Write($fixed)
    $writer.Dispose()
}
finally {
    $zip.Dispose()
}

if (-not (Test-Path $marker)) {
    $current = Get-ConsumerFiles | ForEach-Object {
        [regex]::Match((Get-Content $_.FullName -Raw), "Include=`"$([regex]::Escape($Package))`" Version=`"([^`"]+)`"").Groups[1].Value
    } | Select-Object -First 1
    Set-Content $marker $current
}

Set-ConsumerVersion $local
dotnet restore (Get-ConsumerSolution).FullName "-p:RestoreAdditionalProjectSources=$feed"
if ($LASTEXITCODE -ne 0) { throw "Restoring $Consumer failed" }
"$Consumer now uses $Package $local. Revert with -Revert before committing there."
