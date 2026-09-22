param()

$ErrorActionPreference = "Stop"

$profilesDirectory =
    Join-Path `
        $PSScriptRoot `
        "customer-profiles"

if (-not (Test-Path $profilesDirectory))
{
    throw "Không tìm thấy thư mục customer profiles: $profilesDirectory"
}

$allowedEditions =
    @(
        "Essential",
        "Standard",
        "Professional"
    )

$allowedReleaseChannels =
    @(
        "Development",
        "CustomerPreview",
        "ReleaseCandidate",
        "Production"
    )

$requiredProperties =
    @(
        "HrProductEdition",
        "HrCustomerCode",
        "HrReleaseChannel",
        "HrBrandProductDisplayName",
        "HrBrandCustomerDisplayName",
        "HrBrandSupportLabel",
        "HrBrandSupportContact",
        "HrFeatureEmployees",
        "HrFeatureOrganization",
        "HrFeatureTimeManagement",
        "HrFeaturePayroll"
    )

$featureProperties =
    @(
        "HrFeatureEmployees",
        "HrFeatureOrganization",
        "HrFeatureTimeManagement",
        "HrFeaturePayroll"
    )

$errors =
    New-Object `
        System.Collections.Generic.List[string]

$catalog =
    New-Object `
        System.Collections.Generic.List[object]

$customerCodes =
    @{}

function Get-PropertyValue
{
    param(
        [xml]$Document,
        [string]$PropertyName
    )

    $node =
        $Document.SelectSingleNode(
            "/Project/PropertyGroup/$PropertyName")

    if ($null -eq $node)
    {
        return $null
    }

    return $node.InnerText.Trim()
}

function ConvertTo-FeatureBoolean
{
    param(
        [string]$Value,
        [string]$ProfileName,
        [string]$PropertyName
    )

    $parsedValue =
        $false

    $parsed =
        [bool]::TryParse(
            $Value,
            [ref]$parsedValue)

    if (-not $parsed)
    {
        $errors.Add(
            "Profile '$ProfileName': $PropertyName phải là true hoặc false.")

        return $null
    }

    return $parsedValue
}

$profileFiles =
    Get-ChildItem `
        -Path $profilesDirectory `
        -Filter "*.props" `
        -File |
    Sort-Object `
        Name

if ($profileFiles.Count -eq 0)
{
    throw "Không tìm thấy customer profile nào."
}

foreach ($profileFile in $profileFiles)
{
    $profileName =
        [System.IO.Path]::GetFileNameWithoutExtension(
            $profileFile.Name)

    if ($profileName -notmatch
        "^[A-Za-z0-9][A-Za-z0-9._-]*$")
    {
        $errors.Add(
            "Profile filename '$($profileFile.Name)' không hợp lệ.")

        continue
    }

    try
    {
        [xml]$profile =
            Get-Content `
                -LiteralPath $profileFile.FullName
    }
    catch
    {
        $errors.Add(
            "Profile '$profileName' không phải XML hợp lệ: $($_.Exception.Message)")

        continue
    }

    if ($null -eq $profile.Project)
    {
        $errors.Add(
            "Profile '$profileName' thiếu phần tử Project.")

        continue
    }

    foreach ($propertyName in $requiredProperties)
    {
        $value =
            Get-PropertyValue `
                $profile `
                $propertyName

        if ([string]::IsNullOrWhiteSpace(
                $value))
        {
            $errors.Add(
                "Profile '$profileName' thiếu hoặc để trống $propertyName.")
        }
    }

    $edition =
        Get-PropertyValue `
            $profile `
            "HrProductEdition"

    $customerCode =
        Get-PropertyValue `
            $profile `
            "HrCustomerCode"

    $releaseChannel =
        Get-PropertyValue `
            $profile `
            "HrReleaseChannel"

    if (-not [string]::IsNullOrWhiteSpace(
            $edition))
    {
        if ($allowedEditions -notcontains
            $edition)
        {
            $errors.Add(
                "Profile '$profileName': edition '$edition' không hợp lệ. Cho phép: $($allowedEditions -join ', ').")
        }
    }

    if (-not [string]::IsNullOrWhiteSpace(
            $releaseChannel))
    {
        if ($allowedReleaseChannels -notcontains
            $releaseChannel)
        {
            $errors.Add(
                "Profile '$profileName': release channel '$releaseChannel' không hợp lệ. Cho phép: $($allowedReleaseChannels -join ', ').")
        }
    }

    if (-not [string]::IsNullOrWhiteSpace(
            $customerCode))
    {
        if ($customerCode -notmatch
            "^[A-Za-z0-9][A-Za-z0-9._-]*$")
        {
            $errors.Add(
                "Profile '$profileName': HrCustomerCode '$customerCode' không hợp lệ.")
        }

        $customerCodeKey =
            $customerCode.ToUpperInvariant()

        if ($customerCodes.ContainsKey(
                $customerCodeKey))
        {
            $existingProfile =
                $customerCodes[
                    $customerCodeKey]

            $errors.Add(
                "CustomerCode '$customerCode' bị trùng giữa profile '$existingProfile' và '$profileName'.")
        }
        else
        {
            $customerCodes[
                $customerCodeKey] =
                    $profileName
        }
    }

    $featureValues =
        @{}

    foreach ($featureProperty in $featureProperties)
    {
        $featureText =
            Get-PropertyValue `
                $profile `
                $featureProperty

        if ([string]::IsNullOrWhiteSpace(
                $featureText))
        {
            $featureValues[
                $featureProperty] =
                    $null

            continue
        }

        $featureValues[
            $featureProperty] =
                ConvertTo-FeatureBoolean `
                    $featureText `
                    $profileName `
                    $featureProperty
    }

    $employees =
        $featureValues[
            "HrFeatureEmployees"]

    $organization =
        $featureValues[
            "HrFeatureOrganization"]

    $timeManagement =
        $featureValues[
            "HrFeatureTimeManagement"]

    $payroll =
        $featureValues[
            "HrFeaturePayroll"]

    $catalog.Add(
        [pscustomobject]@{
            Profile =
                $profileName

            Customer =
                $customerCode

            Edition =
                $edition

            Channel =
                $releaseChannel

            Employees =
                if ($employees -eq $true)
                {
                    "ON"
                }
                elseif ($employees -eq $false)
                {
                    "OFF"
                }
                else
                {
                    "INVALID"
                }

            Organization =
                if ($organization -eq $true)
                {
                    "ON"
                }
                elseif ($organization -eq $false)
                {
                    "OFF"
                }
                else
                {
                    "INVALID"
                }

            Time =
                if ($timeManagement -eq $true)
                {
                    "ON"
                }
                elseif ($timeManagement -eq $false)
                {
                    "OFF"
                }
                else
                {
                    "INVALID"
                }

            Payroll =
                if ($payroll -eq $true)
                {
                    "ON"
                }
                elseif ($payroll -eq $false)
                {
                    "OFF"
                }
                else
                {
                    "INVALID"
                }
        })
}

Write-Host ""
Write-Host "Customer profile catalog"
Write-Host "========================"

$catalog |
    Format-Table `
        Profile,
        Customer,
        Edition,
        Channel,
        Employees,
        Organization,
        Time,
        Payroll `
        -AutoSize

if ($errors.Count -gt 0)
{
    Write-Host ""
    Write-Host "Validation errors:"
    Write-Host "=================="

    foreach ($validationError in $errors)
    {
        Write-Host " - $validationError"
    }

    Write-Host ""
    throw "Customer profile validation failed with $($errors.Count) error(s)."
}

Write-Host ""
Write-Host "Validation PASS: $($catalog.Count) customer profile(s)."