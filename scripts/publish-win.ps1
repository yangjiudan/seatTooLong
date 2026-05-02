param(
    [ValidateSet('win-x64')]
    [string]$RuntimeIdentifier = 'win-x64',

    [string]$Configuration = 'Release',

    [switch]$Clean,

    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot 'SeatTooLong.App\SeatTooLong.App.csproj'
$publishRoot = Join-Path $repoRoot 'artifacts\publish'
$publishDir = Join-Path $publishRoot $RuntimeIdentifier

if ($Clean -and (Test-Path $publishDir)) {
    Remove-Item $publishDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Write-Host "Publishing SeatTooLong for $RuntimeIdentifier..."
$publishArgs = @(
    'publish', $appProject,
    '--configuration', $Configuration,
    '--runtime', $RuntimeIdentifier,
    '--self-contained', 'true',
    '--output', $publishDir,
    '-p:PublishSingleFile=false'
)

if ($Version) {
    $publishArgs += @(
        "-p:Version=$Version",
        "-p:AssemblyVersion=$Version",
        "-p:FileVersion=$Version",
        "-p:InformationalVersion=$Version"
    )
}

dotnet @publishArgs

$requiredFiles = @(
    'SeatTooLong.App.exe',
    'haarcascade_frontalface_default.xml'
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $publishDir $file
    if (-not (Test-Path $path)) {
        throw "Publish output is missing required file: $file"
    }
}

$opencvNative = Get-ChildItem -Path $publishDir -Filter 'OpenCvSharpExtern*.dll' -File -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $opencvNative) {
    throw 'Publish output is missing OpenCvSharp native runtime DLLs.'
}

Write-Host "Publish output: $publishDir"
