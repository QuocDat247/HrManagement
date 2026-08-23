namespace HrManagement.Application.Attendance.Expectations;

public interface IWorkExpectationResolver
{
    Task<ResolvedWorkExpectation?> ResolveAsync(
        Guid workScheduleId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ResolvedWorkExpectation>>
        ResolveManyAsync(
            IReadOnlyCollection<Guid> workScheduleIds,
            DateOnly workDate,
            CancellationToken cancellationToken = default);
}
