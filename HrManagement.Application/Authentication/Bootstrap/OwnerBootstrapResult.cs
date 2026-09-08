namespace HrManagement.Application.Authentication.Bootstrap;

public sealed record OwnerBootstrapResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
