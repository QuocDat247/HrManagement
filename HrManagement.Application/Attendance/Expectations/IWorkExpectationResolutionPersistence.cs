namespace HrManagement.Application.Attendance.Expectations;

public interface IWorkExpectationResolutionPersistence
{
    Task<WorkExpectationResolutionData> LoadAsync(
        DateOnly workDate,
        IReadOnlyCollection<Guid> workScheduleIds,
        CancellationToken cancellationToken = default);
}
