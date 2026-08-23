namespace HrManagement.Application.Workspaces.HolidayExceptions;

public interface IHolidayExceptionWorkspaceQueryService
{
    Task<HolidayExceptionWorkspaceSnapshot> GetAsync(
        HolidayExceptionWorkspaceQuery query,
        CancellationToken cancellationToken = default);
}
