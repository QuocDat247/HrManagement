using HrManagement.Domain.Attendance.Schedules;

namespace HrManagement.Application.Attendance.Schedules;

public sealed class WorkScheduleManagementService
    : IWorkScheduleManagementService
{
    private readonly IWorkScheduleManagementPersistence
        _persistence;

    private readonly IWorkScheduleDayRepository
        _dayRepository;

    public WorkScheduleManagementService(
        IWorkScheduleManagementPersistence persistence,
        IWorkScheduleDayRepository dayRepository)
    {
        _persistence =
            persistence;

        _dayRepository =
            dayRepository;
    }

    public async Task<WorkScheduleManagementResult> CreateAsync(
        CreateWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        WorkSchedule schedule;

        try
        {
            schedule =
                new WorkSchedule(
                    Guid.NewGuid(),
                    request.Code,
                    request.Name,
                    request.TimeZoneId,
                    isActive: false);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        WorkSchedule? existing =
            await _persistence
                .GetByCodeAsync(
                    schedule.Code,
                    cancellationToken);

        if (existing is not null)
        {
            return Failure(
                "Mã lịch làm việc đã tồn tại.");
        }

        WorkScheduleDay[] days =
            Enum.GetValues<DayOfWeek>()
                .Select(
                    dayOfWeek =>
                        new WorkScheduleDay(
                            Guid.NewGuid(),
                            schedule.Id,
                            dayOfWeek,
                            isWorkingDay: false))
                .ToArray();

        await _persistence.CreateAsync(
            schedule,
            days,
            cancellationToken);

        return Success(
            schedule.Id);
    }

    public async Task<WorkScheduleManagementResult> CloneAsync(
    CloneWorkScheduleRequest request,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.SourceWorkScheduleId ==
            Guid.Empty)
        {
            return Failure(
                "Mẫu lịch nguồn không hợp lệ.");
        }

        WorkSchedule? source =
            await _persistence
                .GetByIdAsync(
                    request.SourceWorkScheduleId,
                    cancellationToken);

        if (source is null)
        {
            return Failure(
                "Không tìm thấy mẫu lịch nguồn.");
        }

        WorkSchedule clone;

        try
        {
            clone =
                new WorkSchedule(
                    Guid.NewGuid(),
                    request.Code,
                    request.Name,
                    source.TimeZoneId,
                    isActive: false);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        WorkSchedule? sameCode =
            await _persistence
                .GetByCodeAsync(
                    clone.Code,
                    cancellationToken);

        if (sameCode is not null)
        {
            return Failure(
                "Mã lịch làm việc đã tồn tại.");
        }

        IReadOnlyList<WorkScheduleDay> sourceDays =
            await _dayRepository
                .GetByWorkScheduleIdAsync(
                    source.Id,
                    cancellationToken);

        if (sourceDays.Count != 7)
        {
            return Failure(
                "Mẫu lịch nguồn phải có đủ 7 ngày để sao chép.");
        }

        WorkScheduleDay[] clonedDays =
            sourceDays
                .Select(
                    sourceDay =>
                        new WorkScheduleDay(
                            Guid.NewGuid(),
                            clone.Id,
                            sourceDay.DayOfWeek,
                            sourceDay.IsWorkingDay,
                            sourceDay.StartTime,
                            sourceDay.EndTime,
                            sourceDay.BreakMinutes))
                .ToArray();

        await _persistence.CreateAsync(
            clone,
            clonedDays,
            cancellationToken);

        return Success(
            clone.Id);
    }

    public async Task<WorkScheduleManagementResult> UpdateAsync(
        UpdateWorkScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.WorkScheduleId ==
            Guid.Empty)
        {
            return Failure(
                "Mã lịch làm việc không hợp lệ.");
        }

        WorkSchedule? existing =
            await _persistence
                .GetByIdAsync(
                    request.WorkScheduleId,
                    cancellationToken);

        if (existing is null)
        {
            return Failure(
                "Không tìm thấy lịch làm việc.");
        }

        WorkSchedule updated;

        try
        {
            updated =
                new WorkSchedule(
                    existing.Id,
                    request.Code,
                    request.Name,
                    request.TimeZoneId,
                    existing.IsActive);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        WorkSchedule? sameCode =
            await _persistence
                .GetByCodeAsync(
                    updated.Code,
                    cancellationToken);

        if (sameCode is not null
            && sameCode.Id !=
                existing.Id)
        {
            return Failure(
                "Mã lịch làm việc đã tồn tại.");
        }

        await _persistence.UpdateAsync(
            updated,
            cancellationToken);

        return Success(
            updated.Id);
    }

    public async Task<WorkScheduleManagementResult> DeactivateAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
        if (workScheduleId ==
            Guid.Empty)
        {
            return Failure(
                "Mã lịch làm việc không hợp lệ.");
        }

        WorkSchedule? existing =
            await _persistence
                .GetByIdAsync(
                    workScheduleId,
                    cancellationToken);

        if (existing is null)
        {
            return Failure(
                "Không tìm thấy lịch làm việc.");
        }

        if (!existing.IsActive)
        {
            return Success(
                existing.Id);
        }

        var deactivated =
            new WorkSchedule(
                existing.Id,
                existing.Code,
                existing.Name,
                existing.TimeZoneId,
                isActive: false);

        await _persistence.UpdateAsync(
            deactivated,
            cancellationToken);

        return Success(
            deactivated.Id);
    }

    public async Task<WorkScheduleManagementResult> ReactivateAsync(
        Guid workScheduleId,
        CancellationToken cancellationToken = default)
    {
        if (workScheduleId ==
            Guid.Empty)
        {
            return Failure(
                "Mã lịch làm việc không hợp lệ.");
        }

        WorkSchedule? existing =
            await _persistence
                .GetByIdAsync(
                    workScheduleId,
                    cancellationToken);

        if (existing is null)
        {
            return Failure(
                "Không tìm thấy lịch làm việc.");
        }

        if (existing.IsActive)
        {
            return Success(
                existing.Id);
        }

        IReadOnlyList<WorkScheduleDay> days =
            await _dayRepository
                .GetByWorkScheduleIdAsync(
                    existing.Id,
                    cancellationToken);

        if (days.Count != 7)
        {
            return Failure(
                "Lịch làm việc phải có đủ 7 ngày trước khi kích hoạt.");
        }

        if (!days.Any(
                day =>
                    day.IsWorkingDay))
        {
            return Failure(
                "Lịch làm việc phải có ít nhất một ngày làm việc trước khi kích hoạt.");
        }

        var reactivated =
            new WorkSchedule(
                existing.Id,
                existing.Code,
                existing.Name,
                existing.TimeZoneId,
                isActive: true);

        await _persistence.UpdateAsync(
            reactivated,
            cancellationToken);

        return Success(
            reactivated.Id);
    }

    private static WorkScheduleManagementResult Success(
        Guid workScheduleId)
    {
        return new WorkScheduleManagementResult(
            true,
            workScheduleId);
    }

    private static WorkScheduleManagementResult Failure(
        string errorMessage)
    {
        return new WorkScheduleManagementResult(
            false,
            null,
            errorMessage);
    }

    public async Task<WorkScheduleManagementResult> DeleteAsync(
    Guid workScheduleId,
    CancellationToken cancellationToken = default)
    {
        if (workScheduleId == Guid.Empty)
        {
            return Failure(
                "Mẫu lịch làm việc không hợp lệ.");
        }

        WorkSchedule? schedule =
            await _persistence
                .GetByIdAsync(
                    workScheduleId,
                    cancellationToken);

        if (schedule is null)
        {
            return Failure(
                "Không tìm thấy mẫu lịch làm việc.");
        }

        if (schedule.IsActive)
        {
            return Failure(
                "Hãy ngừng sử dụng mẫu lịch trước khi xóa.");
        }

        bool isInUse =
            await _persistence
                .IsInUseAsync(
                    schedule.Id,
                    cancellationToken);

        if (isInUse)
        {
            return Failure(
                "Mẫu lịch đã có lịch sử sử dụng. Hãy ngừng sử dụng thay vì xóa.");
        }

        await _persistence
            .DeleteAsync(
                schedule.Id,
                cancellationToken);

        return Success(
            schedule.Id);
    }
}
