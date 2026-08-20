using HrManagement.Domain.Employees;
using HrManagement.Domain.Leave.Policies;
using HrManagement.Domain.Leave.Types;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestEligibilityPolicyTests
{
    [Fact]
    public void OpenEmploymentPeriod_AllowsRequestWithinPeriod()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmploymentPeriod period =
            CreatePeriod(
                employeeId);

        LeaveType leaveType =
            CreateActiveLeaveType();

        LeaveRequestEligibilityPolicy.EnsureCanRequest(
            employeeId,
            period,
            leaveType,
            new DateOnly(
                2026,
                8,
                20),
            new DateOnly(
                2026,
                8,
                22));
    }

    [Fact]
    public void ClosedEmploymentPeriod_AllowsExactBoundaries()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmploymentPeriod period =
            CreatePeriod(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    31));

        LeaveRequestEligibilityPolicy.EnsureCanRequest(
            employeeId,
            period,
            CreateActiveLeaveType(),
            new DateOnly(
                2026,
                1,
                1),
            new DateOnly(
                2026,
                8,
                31));
    }

    [Fact]
    public void EmploymentPeriodForDifferentEmployee_IsRejected()
    {
        EmploymentPeriod period =
            CreatePeriod(
                Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestEligibilityPolicy
                    .EnsureCanRequest(
                        Guid.NewGuid(),
                        period,
                        CreateActiveLeaveType(),
                        new DateOnly(
                            2026,
                            8,
                            20),
                        new DateOnly(
                            2026,
                            8,
                            20)));
    }

    [Fact]
    public void RequestStartingBeforeEmploymentPeriod_IsRejected()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmploymentPeriod period =
            CreatePeriod(
                employeeId);

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestEligibilityPolicy
                    .EnsureCanRequest(
                        employeeId,
                        period,
                        CreateActiveLeaveType(),
                        new DateOnly(
                            2025,
                            12,
                            31),
                        new DateOnly(
                            2026,
                            1,
                            2)));
    }

    [Fact]
    public void RequestEndingAfterClosedEmploymentPeriod_IsRejected()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmploymentPeriod period =
            CreatePeriod(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    31));

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestEligibilityPolicy
                    .EnsureCanRequest(
                        employeeId,
                        period,
                        CreateActiveLeaveType(),
                        new DateOnly(
                            2026,
                            8,
                            31),
                        new DateOnly(
                            2026,
                            9,
                            1)));
    }

    [Fact]
    public void InactiveLeaveType_IsRejected()
    {
        Guid employeeId =
            Guid.NewGuid();

        EmploymentPeriod period =
            CreatePeriod(
                employeeId);

        var leaveType =
            new LeaveType(
                Guid.NewGuid(),
                "OLD",
                "Loại nghỉ cũ",
                isPaid: true,
                isActive: false);

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestEligibilityPolicy
                    .EnsureCanRequest(
                        employeeId,
                        period,
                        leaveType,
                        new DateOnly(
                            2026,
                            8,
                            20),
                        new DateOnly(
                            2026,
                            8,
                            20)));
    }

    private static EmploymentPeriod CreatePeriod(
        Guid employeeId,
        DateOnly? endDate = null)
    {
        return new EmploymentPeriod(
            Guid.NewGuid(),
            employeeId,
            new DateOnly(
                2026,
                1,
                1),
            endDate);
    }

    private static LeaveType CreateActiveLeaveType()
    {
        return new LeaveType(
            Guid.NewGuid(),
            "ANNUAL",
            "Nghỉ phép năm",
            isPaid: true);
    }
}
