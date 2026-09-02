using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Timesheets;

namespace HrManagement.Tests.Payroll;

public sealed class PayrollCalculationInputServiceTests
{
    [Fact]
    public async Task GetAsync_WhenTimesheetIsOpen_FailsBeforePayrollSources()
    {
        TestContext context =
            CreateContext(
                CreateTimesheet(
                    closed:
                        false));

        PayrollCalculationInput result =
            await context.Service.GetAsync(
                2026,
                8);

        Assert.False(
            result.IsStructurallyReady);

        PayrollCalculationIssue issue =
            Assert.Single(
                result.Issues);

        Assert.Equal(
            PayrollCalculationIssueCode.TimesheetNotClosed,
            issue.Code);

        Assert.Equal(
            0,
            context.CompensationSource.CallCount);

        Assert.Equal(
            0,
            context.OvertimeSource.CallCount);
    }

    [Fact]
    public async Task GetAsync_WhenSourcesAreConsistent_ReturnsReadyEmployeeInput()
    {
        Guid employeeId =
            Guid.NewGuid();

        MonthlyTimesheetReadModel timesheet =
            CreateTimesheet(
                true,
                CreateDay(
                    employeeId,
                    new DateOnly(
                        2026,
                        8,
                        10)));

        TestContext context =
            CreateContext(
                timesheet);

        context.CompensationSource.Items =
        [
            Compensation(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    1))
        ];

        context.OvertimeSource.Items =
        [
            new ApprovedOvertimePayrollItem(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    10),
                60)
        ];

        PayrollCalculationInput result =
            await context.Service.GetAsync(
                2026,
                8);

        Assert.True(
            result.IsStructurallyReady);

        Assert.Empty(
            result.Issues);

        PayrollEmployeeCalculationInput employee =
            Assert.Single(
                result.Employees);

        Assert.Single(
            employee.TimesheetDays);

        Assert.Single(
            employee.CompensationSegments);

        ApprovedOvertimePayrollItem overtime =
            Assert.Single(
                employee.ApprovedOvertime);

        Assert.Equal(
            60,
            overtime.ApprovedMinutes);

        Assert.Equal(
            1,
            context.CompensationSource.CallCount);

        Assert.Equal(
            1,
            context.OvertimeSource.CallCount);
    }

    [Fact]
    public async Task GetAsync_WhenCompensationIsMissing_ReturnsReadinessIssue()
    {
        Guid employeeId =
            Guid.NewGuid();

        TestContext context =
            CreateContext(
                CreateTimesheet(
                    true,
                    CreateDay(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10))));

        PayrollCalculationInput result =
            await context.Service.GetAsync(
                2026,
                8);

        Assert.False(
            result.IsStructurallyReady);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    PayrollCalculationIssueCode
                        .MissingCompensation
                && issue.EmployeeId ==
                    employeeId);
    }

    [Fact]
    public async Task GetAsync_WhenCompensationsOverlap_ReturnsReadinessIssue()
    {
        Guid employeeId =
            Guid.NewGuid();

        TestContext context =
            CreateContext(
                CreateTimesheet(
                    true,
                    CreateDay(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            20))));

        context.CompensationSource.Items =
        [
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
                "VND"),

            new EmployeeCompensationSegment(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    16),
                null,
                28_000_000m,
                "VND")
        ];

        PayrollCalculationInput result =
            await context.Service.GetAsync(
                2026,
                8);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    PayrollCalculationIssueCode
                        .OverlappingCompensation);
    }

    [Fact]
    public async Task GetAsync_WhenCurrencyChangesInsidePeriod_ReturnsReadinessIssue()
    {
        Guid employeeId =
            Guid.NewGuid();

        TestContext context =
            CreateContext(
                CreateTimesheet(
                    true,
                    CreateDay(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10)),
                    CreateDay(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            20))));

        context.CompensationSource.Items =
        [
            new EmployeeCompensationSegment(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    1),
                new DateOnly(
                    2026,
                    8,
                    15),
                25_000_000m,
                "VND"),

            new EmployeeCompensationSegment(
                Guid.NewGuid(),
                employeeId,
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    16),
                null,
                2_000m,
                "USD")
        ];

        PayrollCalculationInput result =
            await context.Service.GetAsync(
                2026,
                8);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    PayrollCalculationIssueCode
                        .MixedCompensationCurrency);
    }

    [Fact]
    public async Task GetAsync_WhenApprovedOvertimeHasNoTimesheetDay_ReturnsReadinessIssue()
    {
        Guid employeeId =
            Guid.NewGuid();

        TestContext context =
            CreateContext(
                CreateTimesheet(
                    true,
                    CreateDay(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            10))));

        context.CompensationSource.Items =
        [
            Compensation(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    1))
        ];

        context.OvertimeSource.Items =
        [
            new ApprovedOvertimePayrollItem(
                Guid.NewGuid(),
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    11),
                60)
        ];

        PayrollCalculationInput result =
            await context.Service.GetAsync(
                2026,
                8);

        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    PayrollCalculationIssueCode
                        .OvertimeWithoutTimesheetDay);
    }

    private static TestContext CreateContext(
        MonthlyTimesheetReadModel timesheet)
    {
        var timesheetService =
            new StubTimesheetService
            {
                Result =
                    timesheet
            };

        var compensationSource =
            new StubCompensationSource();

        var overtimeSource =
            new StubOvertimeSource();

        return new TestContext(
            new PayrollCalculationInputService(
                timesheetService,
                compensationSource,
                overtimeSource),
            compensationSource,
            overtimeSource);
    }

    private static MonthlyTimesheetReadModel CreateTimesheet(
        bool closed,
        params MonthlyTimesheetDayItem[] items)
    {
        return new MonthlyTimesheetReadModel(
            2026,
            8,
            closed
                ? Guid.NewGuid()
                : null,
            closed
                ? TimesheetPeriodStatus.Closed
                : TimesheetPeriodStatus.Open,
            items);
    }

    private static MonthlyTimesheetDayItem CreateDay(
        Guid employeeId,
        DateOnly workDate)
    {
        return new MonthlyTimesheetDayItem(
            Guid.NewGuid(),
            employeeId,
            workDate,
            true,
            480,
            AttendanceCalculationStatus.Present,
            480,
            0,
            0,
            0,
            "EMP001",
            "Nguyễn Văn An");
    }

    private static EmployeeCompensationSegment Compensation(
        Guid employeeId,
        DateOnly effectiveFrom)
    {
        return new EmployeeCompensationSegment(
            Guid.NewGuid(),
            employeeId,
            Guid.NewGuid(),
            effectiveFrom,
            null,
            25_000_000m,
            "VND");
    }

    private sealed record TestContext(
        PayrollCalculationInputService Service,
        StubCompensationSource CompensationSource,
        StubOvertimeSource OvertimeSource);

    private sealed class StubTimesheetService
        : IMonthlyTimesheetQueryService
    {
        public required MonthlyTimesheetReadModel Result
        {
            get;
            init;
        }

        public Task<MonthlyTimesheetReadModel> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Result);
        }
    }

    private sealed class StubCompensationSource
        : IEmployeeCompensationQuerySource
    {
        public IReadOnlyList<EmployeeCompensationSegment> Items
        {
            get;
            set;
        } = [];

        public int CallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<EmployeeCompensationSegment>>
            GetForPeriodAsync(
                IReadOnlyCollection<Guid> employeeIds,
                DateOnly periodStart,
                DateOnly periodEnd,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Items);
        }

        public Task<IReadOnlyList<EmployeeCompensationSegment>>
            GetHistoryAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubOvertimeSource
        : IApprovedOvertimePayrollSource
    {
        public IReadOnlyList<ApprovedOvertimePayrollItem> Items
        {
            get;
            set;
        } = [];

        public int CallCount
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<ApprovedOvertimePayrollItem>>
            GetApprovedAsync(
                IReadOnlyCollection<Guid> employeeIds,
                DateOnly periodStart,
                DateOnly periodEnd,
                CancellationToken cancellationToken = default)
        {
            CallCount++;

            return Task.FromResult(
                Items);
        }
    }
}
