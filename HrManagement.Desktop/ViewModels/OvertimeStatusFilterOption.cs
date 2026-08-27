using HrManagement.Domain.Overtime.Requests;

namespace HrManagement.Desktop.ViewModels;

public sealed record OvertimeStatusFilterOption(
    OvertimeRequestStatus? Status,
    string DisplayName);
