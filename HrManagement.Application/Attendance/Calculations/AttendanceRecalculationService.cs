using HrManagement.Application.Attendance.Records;
using HrManagement.Domain.Attendance.Calculations;
using HrManagement.Domain.Attendance.Records;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Domain.Attendance.Corrections;

namespace HrManagement.Application.Attendance.Calculations;

public sealed class AttendanceRecalculationService
    : IAttendanceRecalculationService
{
    private readonly IAttendanceCorrectionPersistence
        _correctionPersistence;

    private readonly IEffectiveAttendanceTimelineResolver
        _timelineResolver;

    private readonly IApprovedLeaveAttendanceResolver
        _approvedLeaveResolver;

    private readonly IAttendanceRecordRepository
        _recordRepository;

    private readonly IAttendanceEventRepository
        _eventRepository;

    private readonly IAttendanceScheduleWindowResolver
        _scheduleWindowResolver;

    private readonly IAttendanceCalculationPersistence
        _persistence;

    private readonly AttendanceAdherencePolicy
        _adherencePolicy;

    public AttendanceRecalculationService(
        IAttendanceRecordRepository recordRepository,
        IAttendanceEventRepository eventRepository,
        IApprovedLeaveAttendanceResolver approvedLeaveResolver,
        IAttendanceScheduleWindowResolver scheduleWindowResolver,
        IAttendanceCalculationPersistence persistence,
        AttendanceAdherencePolicy adherencePolicy,
        IAttendanceCorrectionPersistence correctionPersistence,
        IEffectiveAttendanceTimelineResolver timelineResolver)
    {
        _correctionPersistence =
            correctionPersistence;

        _timelineResolver =
            timelineResolver;

        _approvedLeaveResolver =
            approvedLeaveResolver;

        _recordRepository =
            recordRepository;

        _eventRepository =
            eventRepository;

        _scheduleWindowResolver =
            scheduleWindowResolver;

        _persistence =
            persistence;

        _adherencePolicy =
            adherencePolicy;
    }

    public async Task<RecalculateAttendanceResult>
        RecalculateAsync(
            RecalculateAttendanceRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request.AttendanceRecordId == Guid.Empty)
        {
            return Failure(
                "Mã bản ghi chấm công không hợp lệ.");
        }

        AttendanceRecord? record =
            await _recordRepository
                .GetByIdAsync(
                    request.AttendanceRecordId,
                    cancellationToken);

        if (record is null)
        {
            return Failure(
                "Không tìm thấy bản ghi chấm công.");
        }

        ApprovedLeaveAttendanceInput? approvedLeave;

        try
        {
            approvedLeave =
                await _approvedLeaveResolver
                    .ResolveAsync(
                        record.EmployeeId,
                        record.EmploymentPeriodId,
                        record.WorkDate,
                        cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        IReadOnlyList<AttendanceEvent> events =
            await _eventRepository
                .GetByAttendanceRecordIdAsync(
                    record.Id,
                    cancellationToken);

        IReadOnlyList<AttendanceCorrection> corrections =
    await _correctionPersistence
        .GetByAttendanceRecordIdAsync(
            record.Id,
            cancellationToken);

        IReadOnlyList<EffectiveAttendanceEvent>
            effectiveTimeline;

        try
        {
            effectiveTimeline =
                _timelineResolver.Resolve(
                    record.Id,
                    record.EmployeeId,
                    events,
                    corrections);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        AttendanceEvent[] effectiveEvents =
            effectiveTimeline
                .Select(
                    item =>
                        new AttendanceEvent(
                            item.EventId,
                            record.Id,
                            record.EmployeeId,
                            item.EventType,
                            item.OccurredAtUtc))
                .ToArray();

        int expectedCorrectionRevision =
            corrections.Count == 0
                ? 0
                : corrections[
                    corrections.Count - 1]
                    .Revision;

        DailyAttendanceCalculation dailyCalculation;

        try
        {
            dailyCalculation =
                DailyAttendanceCalculator.Calculate(
                    record,
                    effectiveEvents,
                    hasApprovedLeave:
                        approvedLeave is not null);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        AttendanceScheduleWindow? scheduleWindow =
            null;

        try
        {
            if (dailyCalculation.Status !=
                AttendanceCalculationStatus.ApprovedLeave)
            {
                scheduleWindow =
                    _scheduleWindowResolver.Resolve(
                        record);
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (TimeZoneNotFoundException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidTimeZoneException exception)
        {
            return Failure(
                exception.Message);
        }

        AttendanceScheduleAdherence adherence;

        try
        {
            adherence =
                AttendanceScheduleAdherenceCalculator
                    .Calculate(
                        record,
                        dailyCalculation,
                        scheduleWindow,
                        _adherencePolicy);

            record.ApplyCalculation(
                dailyCalculation,
                adherence);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                exception.Message);
        }

        await _persistence
            .ApplyAsync(
                record,
                events,
                expectedCorrectionRevision,
                cancellationToken);

        return Success(
            record);
    }

    private static RecalculateAttendanceResult Success(
        AttendanceRecord record)
    {
        return new RecalculateAttendanceResult(
            IsSuccessful: true,
            AttendanceRecordId:
                record.Id,
            Status:
                record.Status,
            WorkedMinutes:
                record.WorkedMinutes,
            LateMinutes:
                record.LateMinutes,
            EarlyLeaveMinutes:
                record.EarlyLeaveMinutes);
    }

    private static RecalculateAttendanceResult Failure(
        string errorMessage)
    {
        return new RecalculateAttendanceResult(
            IsSuccessful: false,
            ErrorMessage:
                errorMessage);
    }
}
