using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestDomainTests
{
    [Fact]
    public void ValidSingleDayRequest_StartsPendingAndNormalizesReason()
    {
        Guid id =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        Guid employmentPeriodId =
            Guid.NewGuid();

        Guid leaveTypeId =
            Guid.NewGuid();

        DateOnly date =
            new(
                2026,
                8,
                20);

        DateTime submittedAtUtc =
            Utc(
                2026,
                8,
                10,
                3,
                15);

        var request =
            new LeaveRequest(
                id,
                employeeId,
                employmentPeriodId,
                leaveTypeId,
                date,
                date,
                "  Việc gia đình  ",
                submittedAtUtc);

        Assert.Equal(
            id,
            request.Id);

        Assert.Equal(
            employeeId,
            request.EmployeeId);

        Assert.Equal(
            employmentPeriodId,
            request.EmploymentPeriodId);

        Assert.Equal(
            leaveTypeId,
            request.LeaveTypeId);

        Assert.Equal(
            date,
            request.StartDate);

        Assert.Equal(
            date,
            request.EndDate);

        Assert.Equal(
            "Việc gia đình",
            request.Reason);

        Assert.Equal(
            submittedAtUtc,
            request.SubmittedAtUtc);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            request.Status);
    }

    [Fact]
    public void MultiDayInclusiveRange_IsAccepted()
    {
        LeaveRequest request =
            CreateRequest(
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    24));

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                20),
            request.StartDate);

        Assert.Equal(
            new DateOnly(
                2026,
                8,
                24),
            request.EndDate);
    }

    [Fact]
    public void WhitespaceReason_IsNormalizedToNull()
    {
        var request =
            new LeaveRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    20),
                "   ",
                Utc(
                    2026,
                    8,
                    10,
                    3,
                    15));

        Assert.Null(
            request.Reason);
    }

    [Fact]
    public void EmptyId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveRequest(
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null,
                    Utc(
                        2026,
                        8,
                        10,
                        3,
                        15)));
    }

    [Fact]
    public void EmptyEmployeeId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveRequest(
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null,
                    Utc(
                        2026,
                        8,
                        10,
                        3,
                        15)));
    }

    [Fact]
    public void EmptyEmploymentPeriodId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null,
                    Utc(
                        2026,
                        8,
                        10,
                        3,
                        15)));
    }

    [Fact]
    public void EmptyLeaveTypeId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.Empty,
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null,
                    Utc(
                        2026,
                        8,
                        10,
                        3,
                        15)));
    }

    [Fact]
    public void DefaultStartDate_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequest(
                    default,
                    new DateOnly(
                        2026,
                        8,
                        20)));
    }

    [Fact]
    public void DefaultEndDate_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequest(
                    new DateOnly(
                        2026,
                        8,
                        20),
                    default));
    }

    [Fact]
    public void EndBeforeStart_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequest(
                    new DateOnly(
                        2026,
                        8,
                        21),
                    new DateOnly(
                        2026,
                        8,
                        20)));
    }

    [Fact]
    public void DefaultSubmittedAtUtc_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null,
                    default));
    }

    [Fact]
    public void NonUtcSubmittedAt_IsRejected()
    {
        DateTime localTime =
            new(
                2026,
                8,
                10,
                10,
                15,
                0,
                DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () =>
                new LeaveRequest(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    new DateOnly(
                        2026,
                        8,
                        20),
                    null,
                    localTime));
    }

    private static LeaveRequest CreateRequest(
        DateOnly startDate,
        DateOnly endDate)
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            startDate,
            endDate,
            null,
            Utc(
                2026,
                8,
                10,
                3,
                15));
    }

    private static DateTime Utc(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        return new DateTime(
            year,
            month,
            day,
            hour,
            minute,
            0,
            DateTimeKind.Utc);
    }
}
