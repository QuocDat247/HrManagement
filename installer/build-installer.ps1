param(
    [Parameter()]
    [ValidatePattern(
        "^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    [string]$Profile = "Generic"
)

$ErrorActionPreference = "Stop"

function Get-RequiredXmlValue
{
    param(
        [xml]$Document,
        [string]$XPath,
        [string]$Description
    )

    $node =
        $Document.SelectSingleNode(
            $XPath)

    if ($null -eq $node)
    {
        throw "Không đọc được $Description."
    }

    $value =
        $node.InnerText

    if ([string]::IsNullOrWhiteSpace(
            $value))
    {
        throw "Không đọc được $Description."
    }

    return $value.Trim()
}

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

$profilePath =
    Join-Path `
        $repoRoot `
        "build\customer-profiles\$Profile.props"

if (-not (Test-Path $profilePath))
{
    throw "Không tìm thấy customer profile '$Profile': $profilePath"
}

[xml]$props =
    Get-Content `
        $propsPath

[xml]$profileProps =
    Get-Content `
        $profilePath

$appVersion =
    Get-RequiredXmlValue `
        $props `
        "/Project/PropertyGroup/Version" `
        "Version từ Directory.Build.props"

$fileVersion =
    Get-RequiredXmlValue `
        $props `
        "/Project/PropertyGroup/FileVersion" `
        "FileVersion từ Directory.Build.props"

$customerCode =
    Get-RequiredXmlValue `
        $profileProps `
        "/Project/PropertyGroup/HrCustomerCode" `
        "HrCustomerCode từ customer profile"

$productEdition =
    Get-RequiredXmlValue `
        $profileProps `
        "/Project/PropertyGroup/HrProductEdition" `
        "HrProductEdition từ customer profile"

$releaseChannel =
    Get-RequiredXmlValue `
        $profileProps `
        "/Project/PropertyGroup/HrReleaseChannel" `
        "HrReleaseChannel từ customer profile"

if ($customerCode -notmatch
    "^[A-Za-z0-9][A-Za-z0-9._-]*$")
{
    throw "HrCustomerCode '$customerCode' không hợp lệ cho tên artifact."
}

$desktopProject =
    Join-Path `
        $repoRoot `
        "HrManagement.Desktop\HrManagement.Desktop.csproj"

$publishDir =
    Join-Path `
        $repoRoot `
        "artifacts\publish\profiles\$Profile"

$installerDir =
    Join-Path `
        $repoRoot `
        "artifacts\installer\profiles\$Profile"

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
Write-Host "Profile       : $Profile"
Write-Host "Customer      : $customerCode"
Write-Host "Edition       : $productEdition"
Write-Host "Channel       : $releaseChannel"
Write-Host "Version       : $appVersion"
Write-Host "File version  : $fileVersion"
Write-Host "Compiler      : $iscc"
Write-Host "Publish dir   : $publishDir"
Write-Host "Installer dir : $installerDir"

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
    "-p:HrCustomerProfile=$Profile" `
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

$rawInstallerFileName =
    "HR-Management-Setup-$appVersion.exe"

$rawInstallerPath =
    Join-Path `
        $installerDir `
        $rawInstallerFileName

if (-not (Test-Path $rawInstallerPath))
{
    throw "Không tìm thấy installer sau khi build: $rawInstallerPath"
}

if ($Profile -eq "Generic")
{
    $installerFileName =
        $rawInstallerFileName
}
else
{
    $installerFileName =
        "HR-Management-$customerCode-$appVersion-Setup.exe"
}

$installerPath =
    Join-Path `
        $installerDir `
        $installerFileName

if ($rawInstallerPath -ne
    $installerPath)
{
    Move-Item `
        -LiteralPath $rawInstallerPath `
        -Destination $installerPath
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