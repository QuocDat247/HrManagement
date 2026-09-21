$ErrorActionPreference = "Stop"

$repoRoot =
    Split-Path `
        -Parent `
        $PSScriptRoot

$propsPath =
    Join-Path `
        $repoRoot `
        "Directory.Build.props"

if (-not (Test-Path $propsPath))
{
    throw "Không tìm thấy Directory.Build.props."
}

[xml]$props =
    Get-Content `
        $propsPath

$appVersion =
    [string]$props.Project.PropertyGroup.Version

$fileVersion =
    [string]$props.Project.PropertyGroup.FileVersion

if ([string]::IsNullOrWhiteSpace(
        $appVersion))
{
    throw "Không đọc được Version từ Directory.Build.props."
}

if ([string]::IsNullOrWhiteSpace(
        $fileVersion))
{
    throw "Không đọc được FileVersion từ Directory.Build.props."
}

$desktopProject =
    Join-Path `
        $repoRoot `
        "HrManagement.Desktop\HrManagement.Desktop.csproj"

$publishDir =
    Join-Path `
        $repoRoot `
        "artifacts\publish\win-x64"

$installerDir =
    Join-Path `
        $repoRoot `
        "artifacts\installer"

$installerScript =
    Join-Path `
        $PSScriptRoot `
        "HrManagement.iss"

$isccCandidates =
    @(
        (Join-Path `
            ${env:ProgramFiles} `
            "Inno Setup 7\ISCC.exe"),

        (Join-Path `
            ${env:LOCALAPPDATA} `
            "Programs\Inno Setup 7\ISCC.exe")
    )

$programFilesX86 =
    ${env:ProgramFiles(x86)}

if (-not [string]::IsNullOrWhiteSpace(
        $programFilesX86))
{
    $isccCandidates +=
        Join-Path `
            $programFilesX86 `
            "Inno Setup 7\ISCC.exe"
}

$iscc =
    $isccCandidates |
    Where-Object {
        Test-Path $_
    } |
    Select-Object `
        -First 1

if ([string]::IsNullOrWhiteSpace(
        $iscc))
{
    throw "Không tìm thấy Inno Setup compiler (ISCC.exe)."
}

Write-Host ""
Write-Host "Version      : $appVersion"
Write-Host "File version : $fileVersion"
Write-Host "Compiler     : $iscc"

Remove-Item `
    $publishDir `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

Remove-Item `
    $installerDir `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

New-Item `
    -ItemType Directory `
    -Path $publishDir `
    -Force | Out-Null

New-Item `
    -ItemType Directory `
    -Path $installerDir `
    -Force | Out-Null

Write-Host ""
Write-Host "Publishing HR Management..."

dotnet publish `
    $desktopProject `
    -p:PublishProfile=WindowsX64 `
    -o $publishDir

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed."
}

Write-Host ""
Write-Host "Building installer..."

& $iscc `
    "/DAppVersion=$appVersion" `
    "/DAppFileVersion=$fileVersion" `
    "/DPublishDir=$publishDir" `
    "/DOutputDir=$installerDir" `
    $installerScript

if ($LASTEXITCODE -ne 0)
{
    throw "Inno Setup compilation failed."
}

$installerFileName =
    "HR-Management-Setup-$appVersion.exe"

$installerPath =
    Join-Path `
        $installerDir `
        $installerFileName

if (-not (Test-Path $installerPath))
{
    throw "Không tìm thấy installer sau khi build: $installerPath"
}

$installerHash =
    Get-FileHash `
        $installerPath `
        -Algorithm SHA256

$checksumPath =
    "$installerPath.sha256"

$checksumLine =
    "$($installerHash.Hash.ToLowerInvariant())  $installerFileName"

Set-Content `
    -Path $checksumPath `
    -Value $checksumLine `
    -Encoding ascii

Write-Host ""
Write-Host "Installer created:"
Write-Host $installerPath

Write-Host ""
Write-Host "SHA256:"
Write-Host $installerHash.Hash

Write-Host ""
Write-Host "Checksum file:"
Write-Host $checksumPath