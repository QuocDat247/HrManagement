using HrManagement.Application.Attendance.Schedules;
using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules.Overrides;

public sealed class WorkScheduleDateOverrideManagementService
    : IWorkScheduleDateOverrideManagementService
{
    private readonly IWorkScheduleRepository
        _workScheduleRepository;

    private readonly IWorkScheduleDateOverrideManagementPersistence
        _persistence;

    public WorkScheduleDateOverrideManagementService(
        IWorkScheduleRepository workScheduleRepository,
        IWorkScheduleDateOverrideManagementPersistence persistence)
    {
        _workScheduleRepository =
            workScheduleRepository;

        _persistence =
            persistence;
    }

    public async Task<WorkScheduleDateOverrideManagementResult> CreateAsync(
        CreateWorkScheduleDateOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        WorkScheduleDateOverride item;

        try
        {
            item =
                new WorkScheduleDateOverride(
                    Guid.NewGuid(),
                    request.WorkScheduleId,
                    request.WorkDate,
                    request.IsWorkingDay,
                    request.StartTime,
                    request.EndTime,
                    request.BreakMinutes,
                    request.Note);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        WorkSchedule? schedule =
            await _workScheduleRepository
                .GetByIdAsync(
                    item.WorkScheduleId,
                    cancellationToken);

        if (schedule is null)
        {
            return Failure(
                "Không tìm thấy lịch làm việc.");
        }

        WorkScheduleDateOverride? existing =
            await _persistence
                .GetByScheduleAndDateAsync(
                    item.WorkScheduleId,
                    item.WorkDate,
                    cancellationToken);

        if (existing is not null)
        {
            return Failure(
                "Lịch làm việc đã có ngoại lệ cho ngày này.");
        }

        await _persistence
            .CreateAsync(
                item,
                cancellationToken);

        return Success(
            item.Id);
    }

    public async Task<WorkScheduleDateOverrideManagementResult> UpdateAsync(
        UpdateWorkScheduleDateOverrideRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.WorkScheduleDateOverrideId ==
            Guid.Empty)
        {
            return Failure(
                "Mã ngoại lệ lịch làm việc không hợp lệ.");
        }

        WorkScheduleDateOverride? existing =
            await _persistence
                .GetByIdAsync(
                    request.WorkScheduleDateOverrideId,
                    cancellationToken);

        if (existing is null)
        {
            return Failure(
                "Không tìm thấy ngoại lệ lịch làm việc.");
        }

        WorkScheduleDateOverride updated;

        try
        {
            updated =
                new WorkScheduleDateOverride(
                    existing.Id,
                    existing.WorkScheduleId,
                    existing.WorkDate,
                    request.IsWorkingDay,
                    request.StartTime,
                    request.EndTime,
                    request.BreakMinutes,
                    request.Note);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        await _persistence
            .UpdateAsync(
                updated,
                cancellationToken);

        return Success(
            updated.Id);
    }

    public async Task<WorkScheduleDateOverrideManagementResult> DeleteAsync(
        Guid workScheduleDateOverrideId,
        CancellationToken cancellationToken = default)
    {
        if (workScheduleDateOverrideId ==
            Guid.Empty)
        {
            return Failure(
                "Mã ngoại lệ lịch làm việc không hợp lệ.");
        }

        WorkScheduleDateOverride? existing =
            await _persistence
                .GetByIdAsync(
                    workScheduleDateOverrideId,
                    cancellationToken);

        if (existing is null)
        {
            return Failure(
                "Không tìm thấy ngoại lệ lịch làm việc.");
        }

        await _persistence
            .DeleteAsync(
                existing.Id,
                cancellationToken);

        return Success(
            existing.Id);
    }

    private static WorkScheduleDateOverrideManagementResult Success(
        Guid workScheduleDateOverrideId)
    {
        return new WorkScheduleDateOverrideManagementResult(
            true,
            workScheduleDateOverrideId);
    }

    private static WorkScheduleDateOverrideManagementResult Failure(
        string errorMessage)
    {
        return new WorkScheduleDateOverrideManagementResult(
            false,
            null,
            errorMessage);
    }
}
