using HrManagement.Domain.Leave.Types;

namespace HrManagement.Tests.Leave;

public sealed class LeaveTypeDomainTests
{
    [Fact]
    public void ValidLeaveType_NormalizesValues()
    {
        Guid id =
            Guid.NewGuid();

        var leaveType =
            new LeaveType(
                id,
                " annual ",
                " Nghỉ phép năm ",
                isPaid: true);

        Assert.Equal(
            id,
            leaveType.Id);

        Assert.Equal(
            "ANNUAL",
            leaveType.Code);

        Assert.Equal(
            "Nghỉ phép năm",
            leaveType.Name);

        Assert.True(
            leaveType.IsPaid);

        Assert.True(
            leaveType.IsActive);
    }

    [Fact]
    public void UnpaidLeaveType_IsSupported()
    {
        var leaveType =
            new LeaveType(
                Guid.NewGuid(),
                "UNPAID",
                "Nghỉ không lương",
                isPaid: false);

        Assert.False(
            leaveType.IsPaid);
    }

    [Fact]
    public void InactiveLeaveType_IsSupported()
    {
        var leaveType =
            new LeaveType(
                Guid.NewGuid(),
                "LEGACY",
                "Loại nghỉ cũ",
                isPaid: true,
                isActive: false);

        Assert.False(
            leaveType.IsActive);
    }

    [Fact]
    public void EmptyId_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveType(
                    Guid.Empty,
                    "ANNUAL",
                    "Nghỉ phép năm",
                    isPaid: true));
    }

    [Fact]
    public void EmptyCode_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveType(
                    Guid.NewGuid(),
                    string.Empty,
                    "Nghỉ phép năm",
                    isPaid: true));
    }

    [Fact]
    public void WhitespaceCode_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveType(
                    Guid.NewGuid(),
                    "   ",
                    "Nghỉ phép năm",
                    isPaid: true));
    }

    [Fact]
    public void EmptyName_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveType(
                    Guid.NewGuid(),
                    "ANNUAL",
                    string.Empty,
                    isPaid: true));
    }

    [Fact]
    public void WhitespaceName_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new LeaveType(
                    Guid.NewGuid(),
                    "ANNUAL",
                    "   ",
                    isPaid: true));
    }
}
