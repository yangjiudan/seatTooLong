param(
    [ValidateSet('win-x64')]
    [string]$RuntimeIdentifier = 'win-x64',

    [string]$Configuration = 'Release',

    [switch]$Clean,

    [string]$InnoSetupCompiler,

    [string]$Version
)

$ErrorActionPreference = 'Stop'

function Get-AppVersionFromProps {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PropsPath
    )

    if (-not (Test-Path $PropsPath)) {
        throw "Version source file was not found: $PropsPath"
    }

    [xml]$props = Get-Content -Path $PropsPath -Raw
    $versionNode = $props.SelectSingleNode('//AppVersion')
    if (-not $versionNode -or [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        throw "Directory.Build.props must define a non-empty <AppVersion>."
    }

    return $versionNode.InnerText.Trim()
}

function Test-SemVer {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value -match '^\d+\.\d+\.\d+$'
}

function Set-IslEntry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$Key,

        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $entry = "$Key=$Value"
    $pattern = "(?m)^;?$([regex]::Escape($Key))=.*$"
    if ([regex]::IsMatch($Content, $pattern)) {
        return [regex]::Replace($Content, $pattern, { param($match) $entry }, 1)
    }

    return "$Content`r`n$entry"
}

function New-ChineseSimplifiedInnoMessagesFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InnoSetupCompiler,

        [Parameter(Mandatory = $true)]
        [string]$TranslationsFile,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath
    )

    $compilerDir = Split-Path -Parent $InnoSetupCompiler
    $defaultMessageCandidates = @(
        (Join-Path $compilerDir 'Default.isl'),
        "${env:ProgramFiles(x86)}\Inno Setup 6\Default.isl",
        "$env:ProgramFiles\Inno Setup 6\Default.isl"
    )
    $defaultMessagesFile = $defaultMessageCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $defaultMessagesFile) {
        throw 'Inno Setup Default.isl was not found. Cannot generate Simplified Chinese installer messages.'
    }

    if (-not (Test-Path $TranslationsFile)) {
        throw "Simplified Chinese translations file was not found: $TranslationsFile"
    }

    $content = Get-Content $defaultMessagesFile -Raw
    $translations = Get-Content $TranslationsFile -Raw -Encoding UTF8 | ConvertFrom-Json

    foreach ($property in $translations.PSObject.Properties) {
        $content = Set-IslEntry -Content $content -Key $property.Name -Value $property.Value
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
    Set-Content -Path $OutputPath -Value $content -Encoding UTF8
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$versionPropsPath = Join-Path $repoRoot 'Directory.Build.props'
$publishScript = Join-Path $PSScriptRoot 'publish-win.ps1'
$installerDir = Join-Path $repoRoot 'installer'
$installerScript = Join-Path $repoRoot 'installer\SeatTooLong.iss'
$installerOutput = Join-Path $repoRoot 'artifacts\installer'
$installerLanguageDir = Join-Path $repoRoot 'artifacts\installer-languages'
$translationsFile = Join-Path $repoRoot 'installer\ChineseSimplified.messages.json'
$chineseMessagesFile = Join-Path $installerLanguageDir 'ChineseSimplified.generated.isl'

if (-not $Version) {
    $Version = Get-AppVersionFromProps -PropsPath $versionPropsPath
}

if (-not (Test-SemVer -Value $Version)) {
    throw "Version must follow SemVer X.Y.Z. Actual: $Version"
}

$outputBaseFileName = "SeatTooLong-Setup-x64-$Version"

& $publishScript -RuntimeIdentifier $RuntimeIdentifier -Configuration $Configuration -Clean:$Clean -Version $Version

if (-not $InnoSetupCompiler) {
    $command = Get-Command 'ISCC.exe' -ErrorAction SilentlyContinue
    if ($command) {
        $InnoSetupCompiler = $command.Source
    }
}

if (-not $InnoSetupCompiler) {
    $defaultPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    $InnoSetupCompiler = $defaultPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $InnoSetupCompiler -or -not (Test-Path $InnoSetupCompiler)) {
    throw 'Inno Setup Compiler (ISCC.exe) was not found. Install Inno Setup 6 or pass -InnoSetupCompiler <path-to-ISCC.exe>.'
}

New-Item -ItemType Directory -Force -Path $installerOutput | Out-Null
New-ChineseSimplifiedInnoMessagesFile -InnoSetupCompiler $InnoSetupCompiler -TranslationsFile $translationsFile -OutputPath $chineseMessagesFile

Write-Host "Building installer with $InnoSetupCompiler..."
Push-Location $installerDir
try {
    & $InnoSetupCompiler "/DMyAppVersion=$Version" "/DMyOutputBaseFilename=$outputBaseFileName" $installerScript
}
finally {
    Pop-Location
}

$setupPath = Join-Path $installerOutput "$outputBaseFileName.exe"
if (-not (Test-Path $setupPath)) {
    throw "Installer was not created: $setupPath"
}

Write-Host "Installer output: $setupPath"
