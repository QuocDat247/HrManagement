namespace HrManagement.Application.Employees.Profiles;

public sealed record SaveEmployeePersonalProfileResult(
    bool IsSuccessful,
    string? ErrorMessage = null);
