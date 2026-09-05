$ErrorActionPreference = "Stop"

$repositoryRoot =
    Split-Path -Parent $PSScriptRoot

$desktopProject =
    Join-Path `
        $repositoryRoot `
        "HrManagement.Desktop/HrManagement.Desktop.csproj"

$publishDirectory =
    Join-Path `
        $repositoryRoot `
        "artifacts/publish/win-x64"

$stagingDirectory =
    Join-Path `
        $repositoryRoot `
        "artifacts/staging/win-x64"

$releaseDirectory =
    Join-Path `
        $repositoryRoot `
        "artifacts/release"

$releaseManifestPath =
    Join-Path `
        $releaseDirectory `
        "hrmanagement-preview-win-x64.json"

$checksumPath =
    Join-Path `
        $releaseDirectory `
        "hrmanagement-preview-win-x64.sha256"

Write-Host "== HR Management Preview Release =="

Write-Host "Repository:"
Write-Host "  $repositoryRoot"

Write-Host ""
Write-Host "Cleaning previous artifacts..."

Remove-Item `
    $publishDirectory `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

Remove-Item `
    $stagingDirectory `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

Remove-Item `
    $releaseDirectory `
    -Recurse `
    -Force `
    -ErrorAction SilentlyContinue

New-Item `
    -ItemType Directory `
    -Path $stagingDirectory `
    -Force |
    Out-Null

New-Item `
    -ItemType Directory `
    -Path $releaseDirectory `
    -Force |
    Out-Null

Write-Host ""
Write-Host "Publishing win-x64 self-contained release..."

dotnet publish `
    $desktopProject `
    -p:PublishProfile=WindowsX64 `
    -o $publishDirectory

if ($LASTEXITCODE -ne 0)
{
    throw "dotnet publish failed."
}

$desktopExecutable =
    Join-Path `
        $publishDirectory `
        "HrManagement.Desktop.exe"

if (-not (Test-Path $desktopExecutable))
{
    throw "Published executable was not found."
}

Write-Host ""
Write-Host "Creating installer staging directory..."

Get-ChildItem `
    -Path $publishDirectory `
    -Force |
    Where-Object {
        $_.Extension -ne ".pdb"
    } |
    Copy-Item `
        -Destination $stagingDirectory `
        -Recurse `
        -Force

$stagedExecutable =
    Join-Path `
        $stagingDirectory `
        "HrManagement.Desktop.exe"

if (-not (Test-Path $stagedExecutable))
{
    throw "Staged executable was not found."
}

$pdbFiles =
    Get-ChildItem `
        -Path $stagingDirectory `
        -Filter "*.pdb" `
        -File `
        -Recurse

if ($pdbFiles.Count -ne 0)
{
    throw "PDB files must not exist in release staging."
}

$forbiddenFilePatterns =
    @(
        "*.db",
        "*.db-wal",
        "*.db-shm",
        "theme-settings.json",
        "diagnostic-consent.json"
    )

foreach ($pattern in $forbiddenFilePatterns)
{
    $matches =
        Get-ChildItem `
            -Path $stagingDirectory `
            -Filter $pattern `
            -File `
            -Recurse

    if ($matches.Count -ne 0)
    {
        throw "Forbidden runtime file found: $pattern"
    }
}

$requiredFiles =
    @(
        "HrManagement.Desktop.exe",
        "HrManagement.Desktop.dll",
        "HrManagement.Application.dll",
        "HrManagement.Domain.dll",
        "HrManagement.Infrastructure.dll",
        "Microsoft.Data.Sqlite.dll",
        "Microsoft.EntityFrameworkCore.Sqlite.dll",
        "e_sqlite3.dll"
    )

foreach ($requiredFile in $requiredFiles)
{
    $match =
        Get-ChildItem `
            -Path $stagingDirectory `
            -Filter $requiredFile `
            -File `
            -Recurse |
        Select-Object -First 1

    if ($null -eq $match)
    {
        throw "Required release file missing: $requiredFile"
    }
}

$versionInfo =
    (Get-Item $stagedExecutable).VersionInfo

$expectedProductName =
    "HR Management"

$expectedFileDescription =
    "HR Management desktop application preview"

$expectedProductVersion =
    "0.1.0-preview.1"

$expectedFileVersion =
    "0.1.0.0"

if ($versionInfo.ProductName -ne
    $expectedProductName)
{
    throw "Unexpected ProductName: $($versionInfo.ProductName)"
}

if ($versionInfo.FileDescription -ne
    $expectedFileDescription)
{
    throw "Unexpected FileDescription: $($versionInfo.FileDescription)"
}

if ($versionInfo.ProductVersion -ne
    $expectedProductVersion)
{
    throw "Unexpected ProductVersion: $($versionInfo.ProductVersion)"
}

if ($versionInfo.FileVersion -ne
    $expectedFileVersion)
{
    throw "Unexpected FileVersion: $($versionInfo.FileVersion)"
}

Write-Host ""
Write-Host "Release identity:"
Write-Host "  Product:      $($versionInfo.ProductName)"
Write-Host "  Description:  $($versionInfo.FileDescription)"
Write-Host "  Product ver.: $($versionInfo.ProductVersion)"
Write-Host "  File ver.:    $($versionInfo.FileVersion)"

$stagedFiles =
    Get-ChildItem `
        -Path $stagingDirectory `
        -File `
        -Recurse |
    Sort-Object FullName

$fileCount =
    $stagedFiles.Count

$sizeBytes =
    (
        $stagedFiles |
        Measure-Object `
            -Property Length `
            -Sum
    ).Sum

$sizeMb =
    [Math]::Round(
        $sizeBytes / 1MB,
        1)

$sourceCommitOutput =
    git `
        -C $repositoryRoot `
        rev-parse HEAD

if ($LASTEXITCODE -ne 0)
{
    throw "Unable to resolve source commit."
}

$sourceCommit =
    "$sourceCommitOutput".Trim()

if ([string]::IsNullOrWhiteSpace(
        $sourceCommit))
{
    throw "Source commit is empty."
}

$checksumLines =
    @()

foreach ($file in $stagedFiles)
{
    $relativePath =
        $file.FullName.Substring(
            $stagingDirectory.Length)

    $relativePath =
        $relativePath.TrimStart(
            [char[]]@(
                '\',
                '/'))

    $relativePath =
        $relativePath.Replace(
            '\',
            '/')

    $fileHash =
        Get-FileHash `
            -Path $file.FullName `
            -Algorithm SHA256

    $hash =
        $fileHash.Hash.ToLowerInvariant()

    $checksumLines +=
        "$hash  $relativePath"
}

$utf8NoBom =
    New-Object `
        System.Text.UTF8Encoding(
            $false)

[System.IO.File]::WriteAllLines(
    $checksumPath,
    $checksumLines,
    $utf8NoBom)

$executableHashResult =
    Get-FileHash `
        -Path $stagedExecutable `
        -Algorithm SHA256

$executableHash =
    $executableHashResult.Hash.ToLowerInvariant()

$manifest =
    [ordered]@{
        schemaVersion =
            1

        product =
            $versionInfo.ProductName

        description =
            $versionInfo.FileDescription

        productVersion =
            $versionInfo.ProductVersion

        fileVersion =
            $versionInfo.FileVersion

        runtimeIdentifier =
            "win-x64"

        selfContained =
            $true

        sourceCommit =
            $sourceCommit

        executable =
            "HrManagement.Desktop.exe"

        executableSha256 =
            $executableHash

        fileCount =
            $fileCount

        sizeBytes =
            [long]$sizeBytes

        checksumFile =
            "hrmanagement-preview-win-x64.sha256"
    }

$manifestJson =
    $manifest |
    ConvertTo-Json `
        -Depth 4

[System.IO.File]::WriteAllText(
    $releaseManifestPath,
    $manifestJson,
    $utf8NoBom)

Write-Host ""
Write-Host "Release staging ready:"
Write-Host "  Path:  $stagingDirectory"
Write-Host "  Files: $fileCount"
Write-Host "  Size:  $sizeMb MB"

Write-Host ""
Write-Host "Release metadata:"
Write-Host "  Commit:     $sourceCommit"
Write-Host "  EXE SHA256: $executableHash"
Write-Host "  Manifest:   $releaseManifestPath"
Write-Host "  Checksums:  $checksumPath"