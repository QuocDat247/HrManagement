using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Tests.Overtime;

public sealed class OvertimeRequestTests
{
    [Fact]
    public void Constructor_CreatesPendingRequest()
    {
        OvertimeRequest request =
            CreateRequest(
                requestedMinutes: 120,
                reason:
                    "  Hoàn thành bản phát hành  ");

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            request.Status);

        Assert.Equal(
            120,
            request.RequestedMinutes);

        Assert.Equal(
            "Hoàn thành bản phát hành",
            request.Reason);

        Assert.Null(
            request.ApprovedMinutes);
    }

    [Fact]
    public void Constructor_WhenRequestedMinutesIsInvalid_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                CreateRequest(
                    requestedMinutes: 0));
    }

    [Fact]
    public void TransitionTo_Approved_StoresApprovedMinutesAndHistory()
    {
        OvertimeRequest request =
            CreateRequest(
                requestedMinutes: 120);

        OvertimeRequestStatusChange change =
            request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Approved,
                Utc(),
                " user-1 ",
                " admin ",
                approvedMinutes: 90,
                note:
                    " Duyệt một phần ");

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            request.Status);

        Assert.Equal(
            90,
            request.ApprovedMinutes);

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            change.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            change.NewStatus);

        Assert.Equal(
            90,
            change.ApprovedMinutes);

        Assert.Equal(
            "user-1",
            change.ChangedByUserId);

        Assert.Equal(
            "admin",
            change.ChangedByUsername);

        Assert.Equal(
            "Duyệt một phần",
            change.Note);
    }

    [Fact]
    public void TransitionTo_ApprovedAboveRequestedMinutes_RejectsWithoutChangingStatus()
    {
        OvertimeRequest request =
            CreateRequest(
                requestedMinutes: 60);

        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    OvertimeRequestStatus.Approved,
                    Utc(),
                    "user-1",
                    "admin",
                    approvedMinutes: 90));

        Assert.Equal(
            OvertimeRequestStatus.Pending,
            request.Status);

        Assert.Null(
            request.ApprovedMinutes);
    }

    [Fact]
    public void TransitionTo_ApprovedThenCancelled_ClearsCurrentApprovedMinutes()
    {
        OvertimeRequest request =
            CreateRequest(
                requestedMinutes: 120);

        request.TransitionTo(
            Guid.NewGuid(),
            OvertimeRequestStatus.Approved,
            Utc(),
            "user-1",
            "admin",
            approvedMinutes: 120);

        OvertimeRequestStatusChange cancellation =
            request.TransitionTo(
                Guid.NewGuid(),
                OvertimeRequestStatus.Cancelled,
                Utc(
                    hour: 13),
                "user-1",
                "admin");

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            request.Status);

        Assert.Null(
            request.ApprovedMinutes);

        Assert.Equal(
            OvertimeRequestStatus.Approved,
            cancellation.PreviousStatus);

        Assert.Equal(
            OvertimeRequestStatus.Cancelled,
            cancellation.NewStatus);
    }

    [Fact]
    public void TransitionTo_FromRejectedToApproved_IsNotAllowed()
    {
        OvertimeRequest request =
            CreateRequest(
                requestedMinutes: 60);

        request.TransitionTo(
            Guid.NewGuid(),
            OvertimeRequestStatus.Rejected,
            Utc(),
            "user-1",
            "admin");

        Assert.Throws<InvalidOperationException>(
            () =>
                request.TransitionTo(
                    Guid.NewGuid(),
                    OvertimeRequestStatus.Approved,
                    Utc(
                        hour: 13),
                    "user-1",
                    "admin",
                    approvedMinutes: 60));

        Assert.Equal(
            OvertimeRequestStatus.Rejected,
            request.Status);
    }

    private static OvertimeRequest CreateRequest(
        int requestedMinutes = 60,
        string? reason = null)
    {
        return new OvertimeRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                8,
                27),
            requestedMinutes,
            reason,
            Utc());
    }

    private static DateTime Utc(
        int hour = 12)
    {
        return new DateTime(
            2026,
            8,
            27,
            hour,
            0,
            0,
            DateTimeKind.Utc);
    }
}
