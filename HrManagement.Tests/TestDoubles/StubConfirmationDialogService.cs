using HrManagement.Desktop.Services;

namespace HrManagement.Tests.TestDoubles;

public sealed class StubConfirmationDialogService
    : IConfirmationDialogService
{
    public bool Result
    {
        get;
        set;
    } = true;

    public int ConfirmCallCount
    {
        get;
        private set;
    }

    public string? LastTitle
    {
        get;
        private set;
    }

    public string? LastMessage
    {
        get;
        private set;
    }

    public bool Confirm(
        string title,
        string message)
    {
        ConfirmCallCount++;

        LastTitle =
            title;

        LastMessage =
            message;

        return Result;
    }
}
