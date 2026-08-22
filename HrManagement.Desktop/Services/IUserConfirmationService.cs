namespace HrManagement.Desktop.Services;

public interface IUserConfirmationService
{
    bool Confirm(
        string title,
        string message);
}
