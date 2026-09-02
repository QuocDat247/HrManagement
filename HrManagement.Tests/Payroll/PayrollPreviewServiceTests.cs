using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Attendance.Calculations;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollPreviewServiceTests
{
    [Fact]
    public async Task GetAsync_WhenEmployeeHasNoOvertime_ReturnsFinalizableBaseSalaryPreview()
    {
        Guid employeeId =
            Guid.NewGuid();

        PayrollEmployeeCalculationInput employee =
            EmployeeInput(
                employeeId);

        var inputService =
            new StubInputService(
                ReadyInput(
                    employee));

        var service =
            CreateService(
                inputService);

        PayrollPreview preview =
            await service.GetAsync(
                2026,
                8);

        Assert.True(
            preview.IsFinalizable);

        Assert.Empty(
            preview.Issues);

        PayrollEmployeePreview row =
            Assert.Single(
                preview.Employees);

        Assert.Equal(
            25_000_000m,
            row.BaseSalaryAmount);

        Assert.Equal(
            0,
            row.ApprovedOvertimeMinutes);

        Assert.Equal(
            0,
            row.PayableOvertimeMinutes);

        Assert.Equal(
            0m,
            row.OvertimeAmount);

        Assert.Equal(
            25_000_000m,
            row.GrossAmount);
    }

    [Fact]
    public async Task GetAsync_WhenPayableOvertimeExists_BlocksFinalizationUntilRateIsConfigured()
    {
        Guid employeeId =
            Guid.NewGuid();

        PayrollEmployeeCalculationInput employee =
            EmployeeInput(
                employeeId,
                withOvertime:
                    true);

        var service =
            CreateService(
                new StubInputService(
                    ReadyInput(
                        employee)));

        PayrollPreview preview =
            await service.GetAsync(
                2026,
                8);

        Assert.False(
            preview.IsFinalizable);

        PayrollEmployeePreview row =
            Assert.Single(
                preview.Employees);

        Assert.Equal(
            120,
            row.ApprovedOvertimeMinutes);

        Assert.Equal(
            120,
            row.PayableOvertimeMinutes);

        Assert.Null(
            row.OvertimeAmount);

        Assert.Null(
            row.GrossAmount);

        Assert.Contains(
            row.Issues,
            issue =>
                issue.Code ==
                    PayrollCalculationIssueCode
                        .OvertimePayRateNotConfigured);
    }

    [Fact]
    public async Task GetAsync_WhenStructuralInputIsNotReady_ReturnsIssuesWithoutEmployeePreview()
    {
        PayrollCalculationInput input =
            new(
                2026,
                8,
                null,
                false,
                [],
                [
                    new PayrollCalculationIssue(
                        PayrollCalculationIssueCode
                            .TimesheetNotClosed,
                        null,
                        "Kỳ công chưa đóng.")
                ]);

        var service =
            CreateService(
                new StubInputService(
                    input));

        PayrollPreview preview =
            await service.GetAsync(
                2026,
                8);

        Assert.False(
            preview.IsFinalizable);

        Assert.Empty(
            preview.Employees);

        Assert.Single(
            preview.Issues);
    }

    private static PayrollPreviewService CreateService(
        IPayrollCalculationInputService inputService)
    {
        return new PayrollPreviewService(
            inputService,
            new CalendarDayBaseSalaryProrationPolicy(),
            new EmployeeOvertimePayabilityResolver(
                new ConservativeOvertimePayabilityPolicy()),
            new UnconfiguredOvertimeAmountPolicy(),
            new CurrencyMoneyRoundingPolicy());
    }

    private static PayrollCalculationInput ReadyInput(
        PayrollEmployeeCalculationInput employee)
    {
        return new PayrollCalculationInput(
            2026,
            8,
            Guid.NewGuid(),
            true,
            [employee],
            []);
    }

    private static PayrollEmployeeCalculationInput EmployeeInput(
    Guid employeeId,
    bool withOvertime = false)
    {
        DateOnly overtimeDate =
            new(
                2026,
                8,
                10);

        MonthlyTimesheetDayItem[] days =
            Enumerable
                .Range(
                    1,
                    DateTime.DaysInMonth(
                        2026,
                        8))
                .Select(
                    dayNumber =>
                    {
                        DateOnly workDate =
                            new(
                                2026,
                                8,
                                dayNumber);

                        int workedMinutes =
                            withOvertime
                            && workDate == overtimeDate
                                ? 120
                                : 0;

                        return new MonthlyTimesheetDayItem(
                            Guid.NewGuid(),
                            employeeId,
                            workDate,
                            false,
                            0,
                            AttendanceCalculationStatus
                                .NonWorkingDay,
                            workedMinutes,
                            0,
                            0,
                            0,
                            "EMP001",
                            "Nguyễn Văn An");
                    })
                .ToArray();

        var compensation =
            new EmployeeCompensationSegment(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1),
                null,
                25_000_000m,
                "VND");

        ApprovedOvertimePayrollItem[] overtime =
            withOvertime
                ?
                [
                    new ApprovedOvertimePayrollItem(
                    Guid.NewGuid(),
                    employeeId,
                    overtimeDate,
                    120)
                ]
                : [];

        return new PayrollEmployeeCalculationInput(
            employeeId,
            "EMP001",
            "Nguyễn Văn An",
            days,
            [compensation],
            overtime);
    }

    private sealed class StubInputService
        : IPayrollCalculationInputService
    {
        private readonly PayrollCalculationInput
            _result;

        public StubInputService(
            PayrollCalculationInput result)
        {
            _result =
                result;
        }

        public Task<PayrollCalculationInput> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                _result);
        }
    }
}
