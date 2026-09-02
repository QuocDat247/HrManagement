namespace HrManagement.Domain.Payroll.Compensation;

public sealed class EmployeeCompensation
{
    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public Guid EmploymentPeriodId
    {
        get;
    }

    public DateOnly EffectiveFrom
    {
        get;
    }

    public DateOnly? EffectiveTo
    {
        get;
        private set;
    }

    public decimal MonthlyBaseSalary
    {
        get;
    }

    public string CurrencyCode
    {
        get;
    }

    public bool IsOpen =>
        EffectiveTo is null;

    public EmployeeCompensation(
        Guid id,
        Guid employeeId,
        Guid employmentPeriodId,
        DateOnly effectiveFrom,
        decimal monthlyBaseSalary,
        string currencyCode,
        DateOnly? effectiveTo = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã cấu hình lương không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (employmentPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giai đoạn làm việc không hợp lệ.",
                nameof(employmentPeriodId));
        }

        if (effectiveFrom == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu hiệu lực lương không hợp lệ.",
                nameof(effectiveFrom));
        }

        if (monthlyBaseSalary < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monthlyBaseSalary),
                "Lương cơ bản tháng không được âm.");
        }

        string normalizedCurrencyCode =
            NormalizeCurrencyCode(
                currencyCode);

        if (effectiveTo.HasValue
            && effectiveTo.Value <
                effectiveFrom)
        {
            throw new ArgumentException(
                "Ngày kết thúc hiệu lực lương không thể trước ngày bắt đầu.",
                nameof(effectiveTo));
        }

        Id =
            id;

        EmployeeId =
            employeeId;

        EmploymentPeriodId =
            employmentPeriodId;

        EffectiveFrom =
            effectiveFrom;

        EffectiveTo =
            effectiveTo;

        MonthlyBaseSalary =
            monthlyBaseSalary;

        CurrencyCode =
            normalizedCurrencyCode;
    }

    public void Close(
        DateOnly effectiveTo)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException(
                "Cấu hình lương đã được kết thúc.");
        }

        if (effectiveTo == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc hiệu lực lương không hợp lệ.",
                nameof(effectiveTo));
        }

        if (effectiveTo <
            EffectiveFrom)
        {
            throw new ArgumentException(
                "Ngày kết thúc hiệu lực lương không thể trước ngày bắt đầu.",
                nameof(effectiveTo));
        }

        EffectiveTo =
            effectiveTo;
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
