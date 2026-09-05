$ErrorActionPreference = "Stop"

$repositoryRoot =
    Split-Path -Parent $PSScriptRoot

$publishScript =
    Join-Path `
        $PSScriptRoot `
        "publish-preview.ps1"

$installerScript =
    Join-Path `
        $repositoryRoot `
        "installer/HrManagementPreview.iss"

$installerOutput =
    Join-Path `
        $repositoryRoot `
        "artifacts/installer/Setup.exe"

$releaseDirectory =
    Join-Path `
        $repositoryRoot `
        "artifacts/release"

$installerChecksumPath =
    Join-Path `
        $releaseDirectory `
        "Setup.exe.sha256"

Write-Host "== HR Management Preview Installer =="

if (-not (Test-Path $publishScript))
{
    throw "Preview publish script was not found."
}

if (-not (Test-Path $installerScript))
{
    throw "Inno Setup definition was not found."
}

Write-Host ""
Write-Host "Building release staging..."

& powershell.exe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $publishScript

if ($LASTEXITCODE -ne 0)
{
    throw "Preview release pipeline failed."
}

Write-Host ""
Write-Host "Locating Inno Setup compiler..."

$isccCandidates =
    @(
        (Join-Path `
            $env:LOCALAPPDATA `
            "Programs\Inno Setup 6\ISCC.exe"),

        (Join-Path `
            $env:ProgramFiles `
            "Inno Setup 6\ISCC.exe")
    )

if (${env:ProgramFiles(x86)})
{
    $isccCandidates +=
        Join-Path `
            ${env:ProgramFiles(x86)} `
            "Inno Setup 6\ISCC.exe"
}

$iscc =
    $isccCandidates |
    Where-Object {
        Test-Path $_
    } |
    Select-Object -First 1

if ($null -eq $iscc)
{
    $registryRoots =
        @(
            "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
            "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*",
            "HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*"
        )

    $innoInstallation =
        Get-ItemProperty `
            $registryRoots `
            -ErrorAction SilentlyContinue |
        Where-Object {
            $_.DisplayName -like "Inno Setup*"
        } |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace(
                $_.InstallLocation)
        } |
        Select-Object -First 1

    if ($null -ne $innoInstallation)
    {
        $candidate =
            Join-Path `
                $innoInstallation.InstallLocation `
                "ISCC.exe"

        if (Test-Path $candidate)
        {
            $iscc =
                $candidate
        }
    }
}

if ($null -eq $iscc)
{
    throw "Inno Setup 6 compiler (ISCC.exe) was not found."
}

Write-Host "  $iscc"

Write-Host ""
Write-Host "Compiling Setup.exe..."

& $iscc $installerScript

if ($LASTEXITCODE -ne 0)
{
    throw "Inno Setup compilation failed."
}

if (-not (Test-Path $installerOutput))
{
    throw "Setup.exe was not produced."
}

$versionInfo =
    (Get-Item $installerOutput).VersionInfo

$actualProductName =
    ([string]$versionInfo.ProductName).Trim()

$actualFileDescription =
    ([string]$versionInfo.FileDescription).Trim()

$actualProductVersion =
    ([string]$versionInfo.ProductVersion).Trim()

$actualFileVersion =
    ([string]$versionInfo.FileVersion).Trim()

$expectedProductName =
    "HR Management"

$expectedFileDescription =
    "HR Management Preview Setup"

$expectedProductVersion =
    "0.1.0-preview.1"

$expectedFileVersion =
    "0.1.0.0"

if ($actualProductName -ne
    $expectedProductName)
{
    throw `
        "Unexpected installer ProductName. Expected='$expectedProductName' Actual='$actualProductName'"
}

if ($actualFileDescription -ne
    $expectedFileDescription)
{
    throw `
        "Unexpected installer FileDescription. Expected='$expectedFileDescription' Actual='$actualFileDescription'"
}

if ($actualProductVersion -ne
    $expectedProductVersion)
{
    throw `
        "Unexpected installer ProductVersion. Expected='$expectedProductVersion' Actual='$actualProductVersion'"
}

if ($actualFileVersion -ne
    $expectedFileVersion)
{
    throw `
        "Unexpected installer FileVersion. Expected='$expectedFileVersion' Actual='$actualFileVersion'"
}

$installerHashResult =
    Get-FileHash `
        -Path $installerOutput `
        -Algorithm SHA256

$installerHash =
    $installerHashResult.Hash.ToLowerInvariant()

$utf8NoBom =
    New-Object `
        System.Text.UTF8Encoding(
            $false)

[System.IO.File]::WriteAllText(
    $installerChecksumPath,
    "$installerHash  Setup.exe",
    $utf8NoBom)

$installerSizeMb =
    [Math]::Round(
        (Get-Item $installerOutput).Length / 1MB,
        1)

Write-Host ""
Write-Host "Installer ready:"
Write-Host "  File:        $installerOutput"
Write-Host "  Size:        $installerSizeMb MB"
Write-Host "  Product:     $actualProductName"
Write-Host "  Version:     $actualProductVersion"
Write-Host "  File ver.:   $actualFileVersion"
Write-Host "  SHA256:      $installerHash"
Write-Host "  Checksum:    $installerChecksumPath"