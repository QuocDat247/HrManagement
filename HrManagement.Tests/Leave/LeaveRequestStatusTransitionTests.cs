using HrManagement.Domain.Leave.Requests;

namespace HrManagement.Tests.Leave;

public sealed class LeaveRequestStatusTransitionTests
{
    [Fact]
    public void PendingToApproved_UpdatesStatusAndCreatesHistory()
    {
        LeaveRequest request =
            CreateRequest();

        DateTime changedAtUtc =
            Utc(
                2026,
                8,
                20,
                6,
                0);

        Guid historyId =
            Guid.NewGuid();

        LeaveRequestStatusChange change =
            request.TransitionTo(
                historyId,
                LeaveRequestStatus.Approved,
                changedAtUtc,
                " admin ",
                " administrator ",
                "  Đã kiểm tra  ");

        Assert.Equal(
            LeaveRequestStatus.Approved,
            request.Status);

        Assert.Equal(
            historyId,
            change.Id);

        Assert.Equal(
            request.Id,
            change.LeaveRequestId);

        Assert.Equal(
            LeaveRequestStatus.Pending,
            change.FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            change.ToStatus);

        Assert.Equal(
            changedAtUtc,
            change.ChangedAtUtc);

        Assert.Equal(
            "admin",
            change.ChangedByUserId);

        Assert.Equal(
            "administrator",
            change.ChangedByUsername);

        Assert.Equal(
            "Đã kiểm tra",
            change.Note);
    }

    [Fact]
    public void PendingToRejected_IsAllowed()
    {
        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Rejected,
            Utc(2026, 8, 20, 6, 0),
            "admin",
            "admin");

        Assert.Equal(
            LeaveRequestStatus.Rejected,
            request.Status);
    }

    [Fact]
    public void PendingToCancelled_IsAllowed()
    {
        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Cancelled,
            Utc(2026, 8, 20, 6, 0),
            "admin",
            "admin");

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            request.Status);
    }

    [Fact]
    public void ApprovedToCancelled_IsAllowed()
    {
        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Approved,
            Utc(2026, 8, 20, 6, 0),
            "admin",
            "admin");

        LeaveRequestStatusChange cancellation =
            request.TransitionTo(
                Guid.NewGuid(),
                LeaveRequestStatus.Cancelled,
                Utc(2026, 8, 20, 7, 0),
                "admin",
                "admin");

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            request.Status);

        Assert.Equal(
            LeaveRequestStatus.Approved,
            cancellation.FromStatus);

        Assert.Equal(
            LeaveRequestStatus.Cancelled,
            cancellation.ToStatus);
    }

    [Fact]
    public void TransitionToSameStatus_IsRejected()
    {
        LeaveRequest request =
            CreateRequest();

        Assert.Throws<InvalidOperationException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Pending,
                    Utc(2026, 8, 20, 6, 0),
                    "admin",
                    "admin"));
    }

    [Fact]
    public void ApprovedToRejected_IsRejected()
    {
        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Approved,
            Utc(2026, 8, 20, 6, 0),
            "admin",
            "admin");

        Assert.Throws<InvalidOperationException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Rejected,
                    Utc(2026, 8, 20, 7, 0),
                    "admin",
                    "admin"));
    }

    [Fact]
    public void RejectedStatus_IsTerminal()
    {
        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Rejected,
            Utc(2026, 8, 20, 6, 0),
            "admin",
            "admin");

        Assert.Throws<InvalidOperationException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Approved,
                    Utc(2026, 8, 20, 7, 0),
                    "admin",
                    "admin"));
    }

    [Fact]
    public void CancelledStatus_IsTerminal()
    {
        LeaveRequest request =
            CreateRequest();

        request.TransitionTo(
            Guid.NewGuid(),
            LeaveRequestStatus.Cancelled,
            Utc(2026, 8, 20, 6, 0),
            "admin",
            "admin");

        Assert.Throws<InvalidOperationException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Approved,
                    Utc(2026, 8, 20, 7, 0),
                    "admin",
                    "admin"));
    }

    [Fact]
    public void NonUtcChangeTime_IsRejectedWithoutChangingStatus()
    {
        LeaveRequest request =
            CreateRequest();

        DateTime local =
            new(
                2026,
                8,
                20,
                13,
                0,
                0,
                DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Approved,
                    local,
                    "admin",
                    "admin"));

        Assert.Equal(
            LeaveRequestStatus.Pending,
            request.Status);
    }

    [Fact]
    public void EmptyActorUserId_IsRejectedWithoutChangingStatus()
    {
        LeaveRequest request =
            CreateRequest();

        Assert.Throws<ArgumentException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    LeaveRequestStatus.Approved,
                    Utc(2026, 8, 20, 6, 0),
                    "   ",
                    "admin"));

        Assert.Equal(
            LeaveRequestStatus.Pending,
            request.Status);
    }

    private static LeaveRequest CreateRequest()
    {
        return new LeaveRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                25),
            new DateOnly(
                2026,
                8,
                26),
            null,
            Utc(
                2026,
                8,
                20,
                4,
                0));
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
