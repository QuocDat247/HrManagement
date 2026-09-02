namespace HrManagement.Domain.Payroll.Snapshots;

public sealed class PayrollEmployeeSnapshot
{
    public Guid Id
    {
        get;
    }

    public Guid PayrollPeriodId
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public string EmployeeCode
    {
        get;
    }

    public string EmployeeFullName
    {
        get;
    }

    public string CurrencyCode
    {
        get;
    }

    public decimal BaseSalaryAmount
    {
        get;
    }

    public int ApprovedOvertimeMinutes
    {
        get;
    }

    public int PayableOvertimeMinutes
    {
        get;
    }

    public decimal OvertimeAmount
    {
        get;
    }

    public decimal GrossAmount
    {
        get;
    }

    public PayrollEmployeeSnapshot(
        Guid id,
        Guid payrollPeriodId,
        Guid employeeId,
        string employeeCode,
        string employeeFullName,
        string currencyCode,
        decimal baseSalaryAmount,
        int approvedOvertimeMinutes,
        int payableOvertimeMinutes,
        decimal overtimeAmount,
        decimal grossAmount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã snapshot lương không hợp lệ.",
                nameof(id));
        }

        if (payrollPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã kỳ lương không hợp lệ.",
                nameof(payrollPeriodId));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        EmployeeCode =
            NormalizeRequired(
                employeeCode,
                30,
                "Mã nhân viên",
                nameof(employeeCode));

        EmployeeFullName =
            NormalizeRequired(
                employeeFullName,
                200,
                "Tên nhân viên",
                nameof(employeeFullName));

        CurrencyCode =
            NormalizeCurrencyCode(
                currencyCode);

        if (baseSalaryAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseSalaryAmount),
                "Tiền lương cơ bản không được âm.");
        }

        if (approvedOvertimeMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(approvedOvertimeMinutes),
                "Số phút tăng ca được duyệt không được âm.");
        }

        if (payableOvertimeMinutes < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(payableOvertimeMinutes),
                "Số phút tăng ca được trả không được âm.");
        }

        if (payableOvertimeMinutes >
            approvedOvertimeMinutes)
        {
            throw new ArgumentException(
                "Số phút tăng ca được trả không thể vượt quá số phút đã duyệt.",
                nameof(payableOvertimeMinutes));
        }

        if (overtimeAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(overtimeAmount),
                "Tiền tăng ca không được âm.");
        }

        if (grossAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(grossAmount),
                "Tổng thu nhập không được âm.");
        }

        decimal expectedGrossAmount =
            baseSalaryAmount
            + overtimeAmount;

        if (grossAmount !=
            expectedGrossAmount)
        {
            throw new ArgumentException(
                "Tổng thu nhập phải bằng lương cơ bản cộng tiền tăng ca.",
                nameof(grossAmount));
        }

        Id =
            id;

        PayrollPeriodId =
            payrollPeriodId;

        EmployeeId =
            employeeId;

        BaseSalaryAmount =
            baseSalaryAmount;

        ApprovedOvertimeMinutes =
            approvedOvertimeMinutes;

        PayableOvertimeMinutes =
            payableOvertimeMinutes;

        OvertimeAmount =
            overtimeAmount;

        GrossAmount =
            grossAmount;
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string fieldName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                $"{fieldName} là bắt buộc.",
                parameterName);
        }

        string normalized =
            value.Trim();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"{fieldName} không được vượt quá {maxLength} ký tự.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeCurrencyCode(
        string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(
                currencyCode))
        {
            throw new ArgumentException(
                "Mã tiền tệ là bắt buộc.",
                nameof(currencyCode));
        }

        string normalized =
            currencyCode
                .Trim()
                .ToUpperInvariant();

        if (normalized.Length != 3
            || normalized.Any(
                character =>
                    character < 'A'
                    || character > 'Z'))
        {
            throw new ArgumentException(
                "Mã tiền tệ phải gồm đúng 3 chữ cái ASCII.",
                nameof(currencyCode));
        }

        return normalized;
    }
}
