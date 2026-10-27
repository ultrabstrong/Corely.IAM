[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$Package,
    [Parameter(Mandatory)][string]$Consumer,
    [switch]$Revert
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$Package = @($Package | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$repoRoot = Split-Path $PSScriptRoot -Parent
$feed = Join-Path ([System.IO.Path]::GetTempPath()) 'corely-local-feed'
New-Item -ItemType Directory -Force $feed | Out-Null

function Get-Marker([string]$PackageId) {
    Join-Path $feed "$PackageId.$((Split-Path $Consumer -Leaf)).original"
}

function Get-CsprojVersion([string]$PackageId) {
    $csproj = Get-ChildItem $repoRoot -Recurse -Filter "$PackageId.csproj" |
        Where-Object FullName -notmatch '\\(bin|obj)\\' | Select-Object -First 1
    if (-not $csproj) { throw "No $PackageId.csproj in $repoRoot" }
    [pscustomobject]@{
        Path = $csproj.FullName
        Version = ([xml](Get-Content $csproj.FullName -Raw)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    }
}

function Get-ConsumerFiles([string]$PackageId) {
    Get-ChildItem $Consumer -Recurse -Include Directory.Packages.props, *.csproj |
        Where-Object { $_.FullName -notmatch '\\(bin|obj|node_modules)\\' } |
        Where-Object { (Get-Content $_.FullName -Raw) -match "Include=`"$([regex]::Escape($PackageId))`" Version=`"" }
}

function Get-ConsumerVersion([string]$PackageId) {
    Get-ConsumerFiles $PackageId | ForEach-Object {
        [regex]::Match((Get-Content $_.FullName -Raw), "Include=`"$([regex]::Escape($PackageId))`" Version=`"([^`"]+)`"").Groups[1].Value
    } | Select-Object -First 1
}

function Set-ConsumerVersion([string]$PackageId, [string]$Version) {
    foreach ($file in Get-ConsumerFiles $PackageId) {
        $text = Get-Content $file.FullName -Raw
        $updated = $text -replace "(Include=`"$([regex]::Escape($PackageId))`" Version=`")[^`"]+", "`${1}$Version"
        if ($updated -ne $text) {
            Set-Content $file.FullName $updated -NoNewline
            "  $($file.FullName.Substring($Consumer.Length + 1)): $PackageId $Version"
        }
    }
}

function Get-ConsumerSolution {
    Get-ChildItem $Consumer -File | Where-Object Extension -in '.slnx', '.sln' | Select-Object -First 1
}

if ($Revert) {
    foreach ($id in $Package) {
        $marker = Get-Marker $id
        if (-not (Test-Path $marker)) { throw "No local build of $id is recorded for $Consumer" }
        $original = (Get-Content $marker -Raw).Trim()
        Set-ConsumerVersion $id $original
        Remove-Item $marker
        "Reverted $id to $original"
    }
    dotnet restore (Get-ConsumerSolution).FullName --no-http-cache | Out-Null
    return
}

$local = "$((Get-CsprojVersion $Package[0]).Version)-local.$(Get-Date -Format 'yyyyMMddHHmmss')"

foreach ($id in $Package) {
    $project = Get-CsprojVersion $id
    $version = "$($project.Version)-local.$($local.Split('-local.')[1])"

    dotnet pack $project.Path -c Release -o $feed "-p:PackageVersion=$version" --force -v q -nologo
    if ($LASTEXITCODE -ne 0) { throw "Packing $id failed" }

    $zip = [System.IO.Compression.ZipFile]::Open((Join-Path $feed "$id.$version.nupkg"), 'Update')
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        $reader = [System.IO.StreamReader]::new($entry.Open())
        $nuspec = $reader.ReadToEnd()
        $reader.Dispose()
        $fixed = [regex]::Replace($nuspec, "<dependency id=`"([^`"]+)`" version=`"$([regex]::Escape($version))`"", {
                param($m)
                $dependency = $m.Groups[1].Value
                $dependencyVersion = if ($Package -contains $dependency) {
                    "$((Get-CsprojVersion $dependency).Version)-local.$($local.Split('-local.')[1])"
                } else {
                    (Get-CsprojVersion $dependency).Version
                }
                "<dependency id=`"$dependency`" version=`"$dependencyVersion`""
            })
        $entry.Delete()
        $writer = [System.IO.StreamWriter]::new($zip.CreateEntry($entry.FullName).Open())
        $writer.Write($fixed)
        $writer.Dispose()
    }
    finally {
        $zip.Dispose()
    }

    $marker = Get-Marker $id
    if (-not (Test-Path $marker)) {
        Set-Content $marker (Get-ConsumerVersion $id)
    }
    Set-ConsumerVersion $id $version
}

dotnet restore (Get-ConsumerSolution).FullName "-p:RestoreAdditionalProjectSources=$feed"
if ($LASTEXITCODE -ne 0) { throw "Restoring $Consumer failed" }
"$Consumer now uses local builds of $($Package -join ', '). Revert with -Revert before committing there."
