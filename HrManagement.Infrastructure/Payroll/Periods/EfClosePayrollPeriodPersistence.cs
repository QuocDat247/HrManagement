using System.Data;
using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Auditing;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Payroll.Snapshots;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Periods;

public sealed class EfClosePayrollPeriodPersistence
    : IClosePayrollPeriodPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IBaseSalaryProrationPolicy
        _baseSalaryPolicy;

    private readonly IEmployeeOvertimePayabilityResolver
        _overtimeResolver;

    private readonly IOvertimeAmountPolicy
        _overtimeAmountPolicy;

    private readonly IMoneyRoundingPolicy
        _moneyRoundingPolicy;

    public EfClosePayrollPeriodPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IBaseSalaryProrationPolicy baseSalaryPolicy,
        IEmployeeOvertimePayabilityResolver overtimeResolver,
        IOvertimeAmountPolicy overtimeAmountPolicy,
        IMoneyRoundingPolicy moneyRoundingPolicy)
    {
        _dbContextFactory =
            dbContextFactory;

        _baseSalaryPolicy =
            baseSalaryPolicy;

        _overtimeResolver =
            overtimeResolver;

        _overtimeAmountPolicy =
            overtimeAmountPolicy;

        _moneyRoundingPolicy =
            moneyRoundingPolicy;
    }

    public async Task PersistAsync(
        PayrollPeriod payrollPeriod,
        IReadOnlyList<PayrollEmployeeSnapshot> snapshots,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            payrollPeriod);

        ArgumentNullException.ThrowIfNull(
            snapshots);

        string normalizedActorUserId =
            NormalizeActor(
                actorUserId,
                100,
                nameof(actorUserId));

        string normalizedActorUsername =
            NormalizeActor(
                actorUsername,
                150,
                nameof(actorUsername));

        ValidateClosedPeriod(
            payrollPeriod,
            normalizedActorUserId,
            normalizedActorUsername);

        ValidateSnapshots(
            payrollPeriod,
            snapshots);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        var timesheetPeriod =
            await dbContext
                .TimesheetPeriods
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    period =>
                        period.Id ==
                        payrollPeriod.TimesheetPeriodId,
                    cancellationToken);

        if (timesheetPeriod is null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy kỳ công nguồn của kỳ lương.");
        }

        if (!timesheetPeriod.IsClosed)
        {
            throw new InvalidOperationException(
                "Không thể đóng kỳ lương khi kỳ công nguồn chưa được đóng.");
        }

        if (timesheetPeriod.Year !=
                payrollPeriod.Year
            || timesheetPeriod.Month !=
                payrollPeriod.Month)
        {
            throw new InvalidOperationException(
                "Kỳ công nguồn không khớp năm tháng của kỳ lương.");
        }

        bool existingPayrollPeriod =
            await dbContext
                .PayrollPeriods
                .AsNoTracking()
                .AnyAsync(
                    period =>
                        period.Id ==
                            payrollPeriod.Id
                        || (
                            period.Year ==
                                payrollPeriod.Year
                            && period.Month ==
                                payrollPeriod.Month
                        )
                        || period.TimesheetPeriodId ==
                            payrollPeriod.TimesheetPeriodId,
                    cancellationToken);

        if (existingPayrollPeriod)
        {
            throw new InvalidOperationException(
                "Kỳ lương này đã được tạo hoặc đã được đóng trước đó.");
        }

        bool existingSnapshots =
            await dbContext
                .PayrollEmployeeSnapshots
                .AsNoTracking()
                .AnyAsync(
                    snapshot =>
                        snapshot.PayrollPeriodId ==
                        payrollPeriod.Id,
                    cancellationToken);

        if (existingSnapshots)
        {
            throw new InvalidOperationException(
                "Đã tồn tại snapshot thuộc mã kỳ lương này.");
        }

        var timesheetRows =
            await dbContext
                .MonthlyTimesheetDaySnapshots
                .AsNoTracking()
                .Where(
                    snapshot =>
                        snapshot.TimesheetPeriodId ==
                        payrollPeriod.TimesheetPeriodId)
                .OrderBy(
                    snapshot =>
                        snapshot.EmployeeId)
                .ThenBy(
                    snapshot =>
                        snapshot.WorkDate)
                .ToArrayAsync(
                    cancellationToken);

        Guid[] sourceEmployeeIds =
            timesheetRows
                .Select(
                    row =>
                        row.EmployeeId)
                .Distinct()
                .OrderBy(
                    employeeId =>
                        employeeId)
                .ToArray();

        Guid[] suppliedEmployeeIds =
            snapshots
                .Select(
                    snapshot =>
                        snapshot.EmployeeId)
                .Distinct()
                .OrderBy(
                    employeeId =>
                        employeeId)
                .ToArray();

        if (!sourceEmployeeIds.SequenceEqual(
                suppliedEmployeeIds))
        {
            throw new InvalidOperationException(
                "Danh sách nhân viên của bảng lương không còn khớp với kỳ công đã đóng.");
        }

        if (sourceEmployeeIds.Length > 0)
        {
            var identities =
                await dbContext
                    .Employees
                    .AsNoTracking()
                    .Where(
                        employee =>
                            sourceEmployeeIds.Contains(
                                employee.Id))
                    .Select(
                        employee =>
                            new
                            {
                                employee.Id,
                                employee.EmployeeCode,
                                employee.FullName
                            })
                    .ToArrayAsync(
                        cancellationToken);

            if (identities.Length !=
                sourceEmployeeIds.Length)
            {
                throw new InvalidOperationException(
                    "Không thể xác định đầy đủ nhân viên của kỳ lương.");
            }

            DateOnly periodStart =
                payrollPeriod.StartDate;

            DateOnly periodEnd =
                payrollPeriod.EndDate;

            var compensations =
                await dbContext
                    .EmployeeCompensations
                    .AsNoTracking()
                    .Where(
                        compensation =>
                            sourceEmployeeIds.Contains(
                                compensation.EmployeeId)
                            && compensation.EffectiveFrom <=
                                periodEnd
                            && (
                                compensation.EffectiveTo == null
                                || compensation.EffectiveTo >=
                                    periodStart
                            ))
                    .OrderBy(
                        compensation =>
                            compensation.EmployeeId)
                    .ThenBy(
                        compensation =>
                            compensation.EffectiveFrom)
                    .ToArrayAsync(
                        cancellationToken);

            var approvedOvertimeRows =
                await dbContext
                    .OvertimeRequests
                    .AsNoTracking()
                    .Where(
                        overtime =>
                            sourceEmployeeIds.Contains(
                                overtime.EmployeeId)
                            && overtime.WorkDate >=
                                periodStart
                            && overtime.WorkDate <=
                                periodEnd
                            && overtime.Status ==
                                OvertimeRequestStatus.Approved)
                    .OrderBy(
                        overtime =>
                            overtime.EmployeeId)
                    .ThenBy(
                        overtime =>
                            overtime.WorkDate)
                    .ThenBy(
                        overtime =>
                            overtime.Id)
                    .ToArrayAsync(
                        cancellationToken);

            var identityByEmployeeId =
                identities.ToDictionary(
                    identity =>
                        identity.Id);

            var suppliedSnapshotByEmployeeId =
                snapshots.ToDictionary(
                    snapshot =>
                        snapshot.EmployeeId);

            foreach (
                Guid employeeId
                in sourceEmployeeIds)
            {
                var identity =
                    identityByEmployeeId[
                        employeeId];

                MonthlyTimesheetDayItem[] days =
                    timesheetRows
                        .Where(
                            row =>
                                row.EmployeeId ==
                                employeeId)
                        .Select(
                            row =>
                                new MonthlyTimesheetDayItem(
                                    row.AttendanceRecordId,
                                    row.EmployeeId,
                                    row.WorkDate,
                                    row.IsWorkingDay,
                                    row.ExpectedPlannedMinutes,
                                    row.Status,
                                    row.WorkedMinutes,
                                    row.LateMinutes,
                                    row.EarlyLeaveMinutes,
                                    row.CorrectionRevision,
                                    identity.EmployeeCode,
                                    identity.FullName))
                        .ToArray();

                EmployeeCompensationSegment[] compensationSegments =
                    compensations
                        .Where(
                            compensation =>
                                compensation.EmployeeId ==
                                employeeId)
                        .Select(
                            compensation =>
                                new EmployeeCompensationSegment(
                                    compensation.Id,
                                    compensation.EmployeeId,
                                    compensation.EmploymentPeriodId,
                                    compensation.EffectiveFrom,
                                    compensation.EffectiveTo,
                                    compensation.MonthlyBaseSalary,
                                    compensation.CurrencyCode))
                        .ToArray();

                ApprovedOvertimePayrollItem[] approvedOvertime =
                    approvedOvertimeRows
                        .Where(
                            overtime =>
                                overtime.EmployeeId ==
                                employeeId)
                        .Select(
                            overtime =>
                            {
                                if (!overtime.ApprovedMinutes.HasValue
                                    || overtime.ApprovedMinutes.Value <= 0
                                    || overtime.ApprovedMinutes.Value >
                                        overtime.RequestedMinutes)
                                {
                                    throw new InvalidOperationException(
                                        "Dữ liệu tăng ca đã duyệt không hợp lệ.");
                                }

                                return new ApprovedOvertimePayrollItem(
                                    overtime.Id,
                                    overtime.EmployeeId,
                                    overtime.WorkDate,
                                    overtime.ApprovedMinutes.Value);
                            })
                        .ToArray();

                var employeeInput =
                    new PayrollEmployeeCalculationInput(
                        employeeId,
                        identity.EmployeeCode,
                        identity.FullName,
                        days,
                        compensationSegments,
                        approvedOvertime);

                BaseSalaryProrationResult baseSalary =
                    _baseSalaryPolicy.Calculate(
                        payrollPeriod.Year,
                        payrollPeriod.Month,
                        compensationSegments,
                        days
                            .Select(
                                day =>
                                    day.WorkDate)
                            .ToArray());

                decimal roundedBaseSalary =
                    _moneyRoundingPolicy.Round(
                        baseSalary.TotalAmount,
                        baseSalary.CurrencyCode);

                IReadOnlyList<OvertimePayabilityResolution>
                    overtimeResolutions =
                        _overtimeResolver.Resolve(
                            employeeInput);

                if (overtimeResolutions.Any(
                        resolution =>
                            !resolution.IsResolved))
                {
                    throw new InvalidOperationException(
                        $"Tăng ca của nhân viên {identity.EmployeeCode} cần được xem xét trước khi đóng kỳ lương.");
                }

                int approvedOvertimeMinutes =
                    approvedOvertime.Sum(
                        overtime =>
                            overtime.ApprovedMinutes);

                int payableOvertimeMinutes =
                    overtimeResolutions.Sum(
                        resolution =>
                            resolution.PayableMinutes
                            ?? 0);

                OvertimeAmountCalculationResult overtimeAmountResult =
                    _overtimeAmountPolicy.Calculate(
                        payrollPeriod.Year,
                        payrollPeriod.Month,
                        employeeInput,
                        overtimeResolutions);

                if (!overtimeAmountResult.IsCalculated
                    || !overtimeAmountResult.Amount.HasValue)
                {
                    throw new InvalidOperationException(
                        overtimeAmountResult.ErrorMessage
                        ?? $"Không thể tính tiền tăng ca cho nhân viên {identity.EmployeeCode}.");
                }

                decimal roundedOvertimeAmount =
                    _moneyRoundingPolicy.Round(
                        overtimeAmountResult.Amount.Value,
                        baseSalary.CurrencyCode);

                decimal roundedGrossAmount =
                    _moneyRoundingPolicy.Round(
                        roundedBaseSalary
                        + roundedOvertimeAmount,
                        baseSalary.CurrencyCode);

                PayrollEmployeeSnapshot suppliedSnapshot =
                    suppliedSnapshotByEmployeeId[
                        employeeId];

                EnsureSnapshotMatchesCurrentSource(
                    suppliedSnapshot,
                    identity.EmployeeCode,
                    identity.FullName,
                    baseSalary.CurrencyCode,
                    roundedBaseSalary,
                    approvedOvertimeMinutes,
                    payableOvertimeMinutes,
                    roundedOvertimeAmount,
                    roundedGrossAmount);
            }
        }

        DateTime occurredAtUtc =
            payrollPeriod.ClosedAtUtc
            ?? throw new InvalidOperationException(
                "Kỳ lương chưa có thời điểm đóng.");

        var audits =
            new List<AuditEntry>(
                snapshots.Count + 1)
            {
                new(
                    Guid.NewGuid(),
                    occurredAtUtc,
                    normalizedActorUserId,
                    normalizedActorUsername,
                    AuditAction.Created,
                    AuditEntityTypes.PayrollPeriod,
                    payrollPeriod.Id)
            };

        foreach (
            PayrollEmployeeSnapshot snapshot
            in snapshots)
        {
            audits.Add(
                new AuditEntry(
                    Guid.NewGuid(),
                    occurredAtUtc,
                    normalizedActorUserId,
                    normalizedActorUsername,
                    AuditAction.Created,
                    AuditEntityTypes.PayrollEmployeeSnapshot,
                    snapshot.Id,
                    snapshot.EmployeeId));
        }

        await dbContext
            .PayrollPeriods
            .AddAsync(
                payrollPeriod,
                cancellationToken);

        if (snapshots.Count > 0)
        {
            await dbContext
                .PayrollEmployeeSnapshots
                .AddRangeAsync(
                    snapshots,
                    cancellationToken);
        }

        await dbContext
            .AuditEntries
            .AddRangeAsync(
                audits,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new InvalidOperationException(
                "Không thể đóng kỳ lương do dữ liệu đã thay đổi đồng thời. Vui lòng tải lại bảng lương và thử lại.",
                exception);
        }
    }

    private static void ValidateClosedPeriod(
        PayrollPeriod payrollPeriod,
        string actorUserId,
        string actorUsername)
    {
        if (!payrollPeriod.IsClosed
            || !payrollPeriod.ClosedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Chỉ kỳ lương đã được đóng ở domain mới được persistence.");
        }

        if (!string.Equals(
                payrollPeriod.ClosedByUserId,
                actorUserId,
                StringComparison.Ordinal)
            || !string.Equals(
                payrollPeriod.ClosedByUsername,
                actorUsername,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Actor đóng kỳ lương không khớp dữ liệu kỳ lương.");
        }
    }

    private static void ValidateSnapshots(
        PayrollPeriod payrollPeriod,
        IReadOnlyList<PayrollEmployeeSnapshot> snapshots)
    {
        if (snapshots.Any(
                snapshot =>
                    snapshot.PayrollPeriodId !=
                    payrollPeriod.Id))
        {
            throw new InvalidOperationException(
                "Snapshot nhân viên không thuộc kỳ lương đang đóng.");
        }

        if (snapshots
                .Select(
                    snapshot =>
                        snapshot.Id)
                .Distinct()
                .Count()
            != snapshots.Count)
        {
            throw new InvalidOperationException(
                "Phát hiện trùng mã snapshot lương.");
        }

        if (snapshots
                .Select(
                    snapshot =>
                        snapshot.EmployeeId)
                .Distinct()
                .Count()
            != snapshots.Count)
        {
            throw new InvalidOperationException(
                "Một nhân viên không thể có nhiều snapshot trong cùng kỳ lương.");
        }
    }

    private static void EnsureSnapshotMatchesCurrentSource(
        PayrollEmployeeSnapshot snapshot,
        string employeeCode,
        string employeeFullName,
        string currencyCode,
        decimal baseSalaryAmount,
        int approvedOvertimeMinutes,
        int payableOvertimeMinutes,
        decimal overtimeAmount,
        decimal grossAmount)
    {
        bool matches =
            string.Equals(
                snapshot.EmployeeCode,
                employeeCode,
                StringComparison.Ordinal)
            && string.Equals(
                snapshot.EmployeeFullName,
                employeeFullName,
                StringComparison.Ordinal)
            && string.Equals(
                snapshot.CurrencyCode,
                currencyCode,
                StringComparison.Ordinal)
            && snapshot.BaseSalaryAmount ==
                baseSalaryAmount
            && snapshot.ApprovedOvertimeMinutes ==
                approvedOvertimeMinutes
            && snapshot.PayableOvertimeMinutes ==
                payableOvertimeMinutes
            && snapshot.OvertimeAmount ==
                overtimeAmount
            && snapshot.GrossAmount ==
                grossAmount;

        if (!matches)
        {
            throw new InvalidOperationException(
                $"Bảng lương xem trước của nhân viên {employeeCode} đã lỗi thời. Vui lòng tải lại trước khi đóng kỳ lương.");
        }
    }

    private static string NormalizeActor(
        string value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            throw new ArgumentException(
                "Actor là bắt buộc.",
                parameterName);
        }

        string normalized =
            value.Trim();

        if (normalized.Length >
            maxLength)
        {
            throw new ArgumentException(
                $"Actor không được vượt quá {maxLength} ký tự.",
                parameterName);
        }

        return normalized;
    }
}
