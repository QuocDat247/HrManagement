param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    [string]$Profile,

    [Parameter(Mandatory = $true)]
    [ValidatePattern("^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    [string]$CustomerCode,

    [Parameter(Mandatory = $true)]
    [string]$CustomerDisplayName,

    [Parameter(Mandatory = $true)]
    [ValidateSet("Essential", "Standard", "Professional")]
    [string]$Edition,

    [Parameter()]
    [ValidateSet("Development", "CustomerPreview", "ReleaseCandidate", "Production")]
    [string]$ReleaseChannel = "CustomerPreview",

    [Parameter()]
    [string]$ProductDisplayName = "HR Management",

    [Parameter()]
    [string]$SupportLabel = "Ho tro trien khai",

    [Parameter(Mandatory = $true)]
    [string]$SupportContact,

    [Parameter()]
    [switch]$DisableEmployees,

    [Parameter()]
    [switch]$DisableOrganization,

    [Parameter()]
    [switch]$DisableTimeManagement,

    [Parameter()]
    [switch]$DisablePayroll
)

$ErrorActionPreference = "Stop"

function Assert-NotWhiteSpace
{
    param(
        [string]$Value,
        [string]$Description
    )

    if ([string]::IsNullOrWhiteSpace(
            $Value))
    {
        throw "$Description must not be empty."
    }
}

function Set-ProfileProperty
{
    param(
        [xml]$Document,
        [string]$PropertyName,
        [string]$Value
    )

    $node =
        $Document.SelectSingleNode(
            "/Project/PropertyGroup/$PropertyName")

    if ($null -eq $node)
    {
        throw "Template is missing property '$PropertyName'."
    }

    $node.InnerText =
        $Value
}

Assert-NotWhiteSpace `
    $CustomerDisplayName `
    "CustomerDisplayName"

Assert-NotWhiteSpace `
    $ProductDisplayName `
    "ProductDisplayName"

Assert-NotWhiteSpace `
    $SupportLabel `
    "SupportLabel"

Assert-NotWhiteSpace `
    $SupportContact `
    "SupportContact"

$templatePath =
    Join-Path `
        $PSScriptRoot `
        "customer-profile-template\CustomerProfile.props.template"

$profilesDirectory =
    Join-Path `
        $PSScriptRoot `
        "customer-profiles"

$validatorPath =
    Join-Path `
        $PSScriptRoot `
        "validate-customer-profiles.ps1"

if (-not (Test-Path $templatePath))
{
    throw "Customer profile template was not found: $templatePath"
}

if (-not (Test-Path $profilesDirectory))
{
    throw "Customer profiles directory was not found: $profilesDirectory"
}

if (-not (Test-Path $validatorPath))
{
    throw "Customer profile validator was not found: $validatorPath"
}

$destinationPath =
    Join-Path `
        $profilesDirectory `
        "$Profile.props"

if (Test-Path $destinationPath)
{
    throw "Customer profile '$Profile' already exists: $destinationPath"
}

Write-Host ""
Write-Host "Validating existing customer profiles..."

& powershell.exe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $validatorPath

if ($LASTEXITCODE -ne 0)
{
    throw "Existing customer profile catalog is invalid."
}

$templateText =
    Get-Content `
        -LiteralPath $templatePath `
        -Raw `
        -Encoding UTF8

[xml]$document =
    $templateText

$featureEmployees =
    if ($DisableEmployees)
    {
        "false"
    }
    else
    {
        "true"
    }

$featureOrganization =
    if ($DisableOrganization)
    {
        "false"
    }
    else
    {
        "true"
    }

$featureTimeManagement =
    if ($DisableTimeManagement)
    {
        "false"
    }
    else
    {
        "true"
    }

$featurePayroll =
    if ($DisablePayroll)
    {
        "false"
    }
    else
    {
        "true"
    }

Set-ProfileProperty `
    $document `
    "HrProductEdition" `
    $Edition

Set-ProfileProperty `
    $document `
    "HrCustomerCode" `
    $CustomerCode

Set-ProfileProperty `
    $document `
    "HrReleaseChannel" `
    $ReleaseChannel

Set-ProfileProperty `
    $document `
    "HrBrandProductDisplayName" `
    $ProductDisplayName

Set-ProfileProperty `
    $document `
    "HrBrandCustomerDisplayName" `
    $CustomerDisplayName

Set-ProfileProperty `
    $document `
    "HrBrandSupportLabel" `
    $SupportLabel

Set-ProfileProperty `
    $document `
    "HrBrandSupportContact" `
    $SupportContact

Set-ProfileProperty `
    $document `
    "HrFeatureEmployees" `
    $featureEmployees

Set-ProfileProperty `
    $document `
    "HrFeatureOrganization" `
    $featureOrganization

Set-ProfileProperty `
    $document `
    "HrFeatureTimeManagement" `
    $featureTimeManagement

Set-ProfileProperty `
    $document `
    "HrFeaturePayroll" `
    $featurePayroll

$writerSettings =
    New-Object `
        System.Xml.XmlWriterSettings

$writerSettings.Indent =
    $true

$writerSettings.Encoding =
    New-Object `
        System.Text.UTF8Encoding `
        -ArgumentList $true

$writer =
    $null

try
{
    $writer =
        [System.Xml.XmlWriter]::Create(
            $destinationPath,
            $writerSettings)

    $document.Save(
        $writer)
}
finally
{
    if ($null -ne $writer)
    {
        $writer.Dispose()
    }
}

Write-Host ""
Write-Host "Profile created:"
Write-Host $destinationPath

Write-Host ""
Write-Host "Validating updated customer profile catalog..."

& powershell.exe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $validatorPath

if ($LASTEXITCODE -ne 0)
{
    Write-Host ""
    Write-Host "Validation failed. Rolling back generated profile..."

    Remove-Item `
        -LiteralPath $destinationPath `
        -Force `
        -ErrorAction SilentlyContinue

    throw "Customer profile '$Profile' is invalid and was rolled back."
}

Write-Host ""
Write-Host "Customer profile created successfully."

Write-Host ""
Write-Host "Next build command:"
Write-Host "dotnet build HrManagement.Desktop/HrManagement.Desktop.csproj -p:HrCustomerProfile=$Profile"

Write-Host ""
Write-Host "Installer command after review/commit:"
Write-Host "powershell -NoProfile -ExecutionPolicy Bypass -File installer/build-installer.ps1 -Profile $Profile"