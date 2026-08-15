namespace HrManagement.Application.Organization.Memberships;

public sealed record OrganizationStaffingCount(
    Guid OrganizationId,
    int ActiveCount,
    int OnLeaveCount,
    int InactiveCount)
{
    public int CurrentEmployeeCount =>
        ActiveCount + OnLeaveCount;

    public int TotalLinkedEmployeeCount =>
        CurrentEmployeeCount + InactiveCount;
}
