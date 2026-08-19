namespace HrManagement.Application.Authentication;

public sealed record AuthenticatedUser(
    string UserId,
    string Username,
    string DisplayName);
