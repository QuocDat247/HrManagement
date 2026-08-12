namespace HrManagement.Domain.Employees;

public sealed class EmploymentHistory
{
    private readonly List<EmploymentPeriod> _periods;

    public Guid EmployeeId { get; }

    public IReadOnlyList<EmploymentPeriod> Periods =>
        _periods;

    public EmploymentPeriod? CurrentPeriod =>
        _periods.LastOrDefault(period =>
            period.IsOpen);

    public EmploymentPeriod? LatestPeriod =>
    _periods.LastOrDefault();

    public EmploymentPeriod ReopenLatestPeriod(
    DateOnly expectedEndDate)
    {
        if (CurrentPeriod is not null)
        {
            throw new InvalidOperationException(
                "Nhân viên đã có giai đoạn làm việc đang mở.");
        }

        EmploymentPeriod latestPeriod =
            LatestPeriod
            ?? throw new InvalidOperationException(
                "Nhân viên chưa có lịch sử làm việc.");

        if (latestPeriod.EndDate != expectedEndDate)
        {
            throw new InvalidOperationException(
                "Ngày kết thúc của lịch sử làm việc không khớp.");
        }

        latestPeriod.Reopen();

        return latestPeriod;
    }

    public EmploymentHistory(
        Guid employeeId,
        IEnumerable<EmploymentPeriod> periods)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        ArgumentNullException.ThrowIfNull(periods);

        List<EmploymentPeriod> orderedPeriods =
            periods
                .OrderBy(period =>
                    period.StartDate)
                .ThenBy(period =>
                    period.Id)
                .ToList();

        ValidateEmployeeOwnership(
            employeeId,
            orderedPeriods);

        ValidateDuplicatePeriodIds(
            orderedPeriods);

        ValidatePeriodsDoNotOverlap(
            orderedPeriods);

        EmployeeId = employeeId;
        _periods = orderedPeriods;
    }

    public EmploymentPeriod CloseCurrentPeriod(
    DateOnly endDate)
    {
        EmploymentPeriod currentPeriod =
            CurrentPeriod
            ?? throw new InvalidOperationException(
                "Nhân viên không có giai đoạn làm việc đang mở.");

        currentPeriod.Close(endDate);

        return currentPeriod;
    }

    private static void ValidateEmployeeOwnership(
        Guid employeeId,
        IReadOnlyList<EmploymentPeriod> periods)
    {
        if (periods.Any(period =>
                period.EmployeeId != employeeId))
        {
            throw new ArgumentException(
                "Lịch sử làm việc chứa giai đoạn thuộc nhân viên khác.",
                nameof(periods));
        }
    }

    private static void ValidateDuplicatePeriodIds(
        IReadOnlyList<EmploymentPeriod> periods)
    {
        bool hasDuplicateIds =
            periods
                .GroupBy(period =>
                    period.Id)
                .Any(group =>
                    group.Count() > 1);

        if (hasDuplicateIds)
        {
            throw new ArgumentException(
                "Lịch sử làm việc chứa giai đoạn bị trùng.",
                nameof(periods));
        }
    }

    private static void ValidatePeriodsDoNotOverlap(
        IReadOnlyList<EmploymentPeriod> periods)
    {
        for (int index = 1;
             index < periods.Count;
             index++)
        {
            EmploymentPeriod previous =
                periods[index - 1];

            EmploymentPeriod current =
                periods[index];

            if (previous.EndDate is null)
            {
                throw new ArgumentException(
                    "Không thể có nhiều hơn một giai đoạn làm việc đang mở.",
                    nameof(periods));
            }

            if (current.StartDate <= previous.EndDate.Value)
            {
                throw new ArgumentException(
                    "Các giai đoạn làm việc không được chồng lấn.",
                    nameof(periods));
            }
        }
    }

    public EmploymentPeriod StartNewPeriod(
    Guid periodId,
    DateOnly startDate)
    {
        if (CurrentPeriod is not null)
        {
            throw new InvalidOperationException(
                "Nhân viên đã có giai đoạn làm việc đang mở.");
        }

        EmploymentPeriod latestPeriod =
            LatestPeriod
            ?? throw new InvalidOperationException(
                "Nhân viên chưa có lịch sử làm việc để tái tuyển dụng.");

        if (!latestPeriod.EndDate.HasValue)
        {
            throw new InvalidOperationException(
                "Giai đoạn làm việc gần nhất chưa được kết thúc.");
        }

        if (startDate <= latestPeriod.EndDate.Value)
        {
            throw new ArgumentException(
                "Ngày tái tuyển dụng phải sau ngày nghỉ việc gần nhất.",
                nameof(startDate));
        }

        var newPeriod =
            new EmploymentPeriod(
                periodId,
                EmployeeId,
                startDate);

        _periods.Add(newPeriod);

        return newPeriod;
    }
}
