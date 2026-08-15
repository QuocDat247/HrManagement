namespace HrManagement.Application.Organization.Memberships;

public interface IOrganizationMembershipQueryService
{
    Task<IReadOnlyList<OrganizationEmployeeListItem>>
        GetEmployeesByDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationEmployeeListItem>>
        GetEmployeesByPositionAsync(
            Guid positionId,
            CancellationToken cancellationToken = default);
}
