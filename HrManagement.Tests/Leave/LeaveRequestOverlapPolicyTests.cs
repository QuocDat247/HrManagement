using HrManagement.Domain.Leave.Policies;
using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestOverlapPolicyTests
{
    [Fact]
    public void NoExistingRequests_IsAllowed()
    {
        LeaveRequestOverlapPolicy.EnsureNoOverlap(
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                20),
            new DateOnly(
                2026,
                8,
                22),
            []);
    }

    [Fact]
    public void AdjacentRequest_IsAllowed()
    {
        Guid employeeId =
            Guid.NewGuid();

        LeaveRequest existing =
            CreateRequest(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        LeaveRequestOverlapPolicy.EnsureNoOverlap(
            employeeId,
            new DateOnly(
                2026,
                8,
                23),
            new DateOnly(
                2026,
                8,
                24),
            [existing]);
    }

    [Fact]
    public void SameDayRequest_IsRejected()
    {
        Guid employeeId =
            Guid.NewGuid();

        LeaveRequest existing =
            CreateRequest(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    20));

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestOverlapPolicy
                    .EnsureNoOverlap(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            20),
                        new DateOnly(
                            2026,
                            8,
                            20),
                        [existing]));
    }

    [Fact]
    public void PartialOverlap_IsRejected()
    {
        Guid employeeId =
            Guid.NewGuid();

        LeaveRequest existing =
            CreateRequest(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestOverlapPolicy
                    .EnsureNoOverlap(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            22),
                        new DateOnly(
                            2026,
                            8,
                            24),
                        [existing]));
    }

    [Fact]
    public void EnclosingExistingRequest_IsRejected()
    {
        Guid employeeId =
            Guid.NewGuid();

        LeaveRequest existing =
            CreateRequest(
                employeeId,
                new DateOnly(
                    2026,
                    8,
                    21),
                new DateOnly(
                    2026,
                    8,
                    22));

        Assert.Throws<InvalidOperationException>(
            () =>
                LeaveRequestOverlapPolicy
                    .EnsureNoOverlap(
                        employeeId,
                        new DateOnly(
                            2026,
                            8,
                            20),
                        new DateOnly(
                            2026,
                            8,
                            23),
                        [existing]));
    }

    [Fact]
    public void RequestForDifferentEmployee_DoesNotBlock()
    {
        LeaveRequest existing =
            CreateRequest(
                Guid.NewGuid(),
                new DateOnly(
                    2026,
                    8,
                    20),
                new DateOnly(
                    2026,
                    8,
                    22));

        LeaveRequestOverlapPolicy.EnsureNoOverlap(
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                20),
            new DateOnly(
                2026,
                8,
                22),
            [existing]);
    }

    private static LeaveRequest CreateRequest(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate)
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            employeeId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            startDate,
            endDate,
            null,
            new DateTime(
                2026,
                8,
                10,
                3,
                0,
                0,
                DateTimeKind.Utc));
    }
}
