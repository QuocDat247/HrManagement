param(
    [Parameter()]
    [ValidatePattern("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    [string]$Profile = "Generic",

    [Parameter()]
    [switch]$RequireCleanSource
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
        throw "Missing $Description."
    }

    $value =
        $node.InnerText

    if ([string]::IsNullOrWhiteSpace(
            $value))
    {
        throw "Empty $Description."
    }

    return $value.Trim()
}

function ConvertTo-RequiredBoolean
{
    param(
        [string]$Value,
        [string]$Description
    )

    $parsedValue =
        $false

    $parsed =
        [bool]::TryParse(
            $Value,
            [ref]$parsedValue)

    if (-not $parsed)
    {
        throw "$Description must be true or false."
    }

    return $parsedValue
}

function Assert-TextEqual
{
    param(
        [string]$Description,
        $Actual,
        $Expected
    )

    $actualText =
        [string]$Actual

    $expectedText =
        [string]$Expected

    $matches =
        [string]::Equals(
            $actualText,
            $expectedText,
            [System.StringComparison]::Ordinal)

    if (-not $matches)
    {
        throw "$Description mismatch. Expected '$expectedText', actual '$actualText'."
    }
}

function Assert-BooleanEqual
{
    param(
        [string]$Description,
        $Actual,
        [bool]$Expected
    )

    if ($null -eq $Actual)
    {
        throw "$Description is missing."
    }

    $actualBoolean =
        [bool]$Actual

    if ($actualBoolean -ne $Expected)
    {
        throw "$Description mismatch. Expected '$Expected', actual '$actualBoolean'."
    }
}

$repoRoot =
    Split-Path `
        -Parent `
        $PSScriptRoot

$validatorPath =
    Join-Path `
        $PSScriptRoot `
        "validate-customer-profiles.ps1"

$propsPath =
    Join-Path `
        $repoRoot `
        "Directory.Build.props"

$profilePath =
    Join-Path `
        $PSScriptRoot `
        "customer-profiles\$Profile.props"

$releaseDirectory =
    Join-Path `
        $repoRoot `
        "artifacts\installer\profiles\$Profile"

$manifestPath =
    Join-Path `
        $releaseDirectory `
        "release-manifest.json"

if (-not (Test-Path $validatorPath))
{
    throw "Customer profile validator was not found: $validatorPath"
}

if (-not (Test-Path $propsPath))
{
    throw "Directory.Build.props was not found."
}

if (-not (Test-Path $profilePath))
{
    throw "Customer profile '$Profile' was not found: $profilePath"
}

if (-not (Test-Path $manifestPath))
{
    throw "Release manifest was not found: $manifestPath"
}

Write-Host ""
Write-Host "Validating customer profile catalog..."

& powershell.exe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $validatorPath

if ($LASTEXITCODE -ne 0)
{
    throw "Customer profile validation failed."
}

[xml]$props =
    Get-Content `
        -LiteralPath $propsPath `
        -Raw `
        -Encoding UTF8

[xml]$profileDocument =
    Get-Content `
        -LiteralPath $profilePath `
        -Raw `
        -Encoding UTF8

$manifest =
    Get-Content `
        -LiteralPath $manifestPath `
        -Raw `
        -Encoding UTF8 |
    ConvertFrom-Json

$appVersion =
    Get-RequiredXmlValue `
        $props `
        "/Project/PropertyGroup/Version" `
        "Version"

$fileVersion =
    Get-RequiredXmlValue `
        $props `
        "/Project/PropertyGroup/FileVersion" `
        "FileVersion"

$customerCode =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrCustomerCode" `
        "HrCustomerCode"

$edition =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrProductEdition" `
        "HrProductEdition"

$releaseChannel =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrReleaseChannel" `
        "HrReleaseChannel"

$productDisplayName =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrBrandProductDisplayName" `
        "HrBrandProductDisplayName"

$customerDisplayName =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrBrandCustomerDisplayName" `
        "HrBrandCustomerDisplayName"

$supportLabel =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrBrandSupportLabel" `
        "HrBrandSupportLabel"

$supportContact =
    Get-RequiredXmlValue `
        $profileDocument `
        "/Project/PropertyGroup/HrBrandSupportContact" `
        "HrBrandSupportContact"

$employees =
    ConvertTo-RequiredBoolean `
        (Get-RequiredXmlValue `
            $profileDocument `
            "/Project/PropertyGroup/HrFeatureEmployees" `
            "HrFeatureEmployees") `
        "HrFeatureEmployees"

$organization =
    ConvertTo-RequiredBoolean `
        (Get-RequiredXmlValue `
            $profileDocument `
            "/Project/PropertyGroup/HrFeatureOrganization" `
            "HrFeatureOrganization") `
        "HrFeatureOrganization"

$timeManagement =
    ConvertTo-RequiredBoolean `
        (Get-RequiredXmlValue `
            $profileDocument `
            "/Project/PropertyGroup/HrFeatureTimeManagement" `
            "HrFeatureTimeManagement") `
        "HrFeatureTimeManagement"

$payroll =
    ConvertTo-RequiredBoolean `
        (Get-RequiredXmlValue `
            $profileDocument `
            "/Project/PropertyGroup/HrFeaturePayroll" `
            "HrFeaturePayroll") `
        "HrFeaturePayroll"

if ([int]$manifest.formatVersion -ne 1)
{
    throw "Unsupported release manifest formatVersion '$($manifest.formatVersion)'."
}

Assert-TextEqual `
    "Manifest product" `
    $manifest.product `
    "HR Management"

Assert-TextEqual `
    "Manifest version" `
    $manifest.version `
    $appVersion

Assert-TextEqual `
    "Manifest fileVersion" `
    $manifest.fileVersion `
    $fileVersion

Assert-TextEqual `
    "Manifest customerProfile" `
    $manifest.customerProfile `
    $Profile

Assert-TextEqual `
    "Manifest customerCode" `
    $manifest.customerCode `
    $customerCode

Assert-TextEqual `
    "Manifest edition" `
    $manifest.edition `
    $edition

Assert-TextEqual `
    "Manifest releaseChannel" `
    $manifest.releaseChannel `
    $releaseChannel

Assert-TextEqual `
    "Brand productDisplayName" `
    $manifest.branding.productDisplayName `
    $productDisplayName

Assert-TextEqual `
    "Brand customerDisplayName" `
    $manifest.branding.customerDisplayName `
    $customerDisplayName

Assert-TextEqual `
    "Brand supportLabel" `
    $manifest.branding.supportLabel `
    $supportLabel

Assert-TextEqual `
    "Brand supportContact" `
    $manifest.branding.supportContact `
    $supportContact

Assert-BooleanEqual `
    "Feature employees" `
    $manifest.enabledFeatures.employees `
    $employees

Assert-BooleanEqual `
    "Feature organization" `
    $manifest.enabledFeatures.organization `
    $organization

Assert-BooleanEqual `
    "Feature timeManagement" `
    $manifest.enabledFeatures.timeManagement `
    $timeManagement

Assert-BooleanEqual `
    "Feature payroll" `
    $manifest.enabledFeatures.payroll `
    $payroll

if ($Profile -eq "Generic")
{
    $expectedInstallerFileName =
        "HR-Management-Setup-$appVersion.exe"
}
else
{
    $expectedInstallerFileName =
        "HR-Management-$customerCode-$appVersion-Setup.exe"
}

Assert-TextEqual `
    "Installer filename" `
    $manifest.installer.fileName `
    $expectedInstallerFileName

$installerPath =
    Join-Path `
        $releaseDirectory `
        $expectedInstallerFileName

if (-not (Test-Path $installerPath))
{
    throw "Installer was not found: $installerPath"
}

$actualHash =
    (
        Get-FileHash `
            -LiteralPath $installerPath `
            -Algorithm SHA256
    ).Hash.ToLowerInvariant()

$manifestHash =
    ([string]$manifest.installer.sha256).ToLowerInvariant()

Assert-TextEqual `
    "Installer SHA256 in manifest" `
    $manifestHash `
    $actualHash

$checksumPath =
    "$installerPath.sha256"

if (-not (Test-Path $checksumPath))
{
    throw "Checksum file was not found: $checksumPath"
}

$checksumText =
    (
        Get-Content `
            -LiteralPath $checksumPath `
            -Raw `
            -Encoding ASCII
    ).Trim()

$expectedChecksumText =
    "$actualHash  $expectedInstallerFileName"

Assert-TextEqual `
    "Checksum file" `
    $checksumText `
    $expectedChecksumText

$currentCommit =
    (
        git `
            -C $repoRoot `
            rev-parse HEAD
    ).Trim()

if ($LASTEXITCODE -ne 0)
{
    throw "Cannot read current Git commit."
}

Assert-TextEqual `
    "Source commit" `
    $manifest.source.commit `
    $currentCommit

$currentStatus =
    git `
        -C $repoRoot `
        status `
        --porcelain `
        --untracked-files=all

if ($LASTEXITCODE -ne 0)
{
    throw "Cannot read Git working tree status."
}

$currentTreeClean =
    [string]::IsNullOrWhiteSpace(
        ($currentStatus -join "`n"))

Assert-BooleanEqual `
    "Source treeClean" `
    $manifest.source.treeClean `
    $currentTreeClean

if ($RequireCleanSource)
{
    if (-not $currentTreeClean)
    {
        throw "A clean source tree is required for this release."
    }

    if (-not [bool]$manifest.source.treeClean)
    {
        throw "Release manifest does not record a clean source tree."
    }
}

$generatedAt =
    [DateTime]::MinValue

$generatedAtValid =
    [DateTime]::TryParse(
        [string]$manifest.generatedAtUtc,
        [ref]$generatedAt)

if (-not $generatedAtValid)
{
    throw "Manifest generatedAtUtc is invalid."
}

Write-Host ""
Write-Host "Customer release verification PASS."
Write-Host ""
Write-Host "Profile   : $Profile"
Write-Host "Customer  : $customerCode"
Write-Host "Version   : $appVersion"
Write-Host "Installer : $expectedInstallerFileName"
Write-Host "SHA256    : $actualHash"
Write-Host "Commit    : $currentCommit"
Write-Host "Tree clean: $currentTreeClean"