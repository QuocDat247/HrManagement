using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Authentication;
using HrManagement.Domain.Attendance.Corrections;
using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Application.Attendance.Corrections;

public sealed class AttendanceCorrectionService
    : IAttendanceCorrectionService
{
    private readonly IAttendanceRecordRepository
        _recordRepository;

    private readonly IAttendanceEventRepository
        _eventRepository;

    private readonly IAttendanceCorrectionPersistence
        _correctionPersistence;

    private readonly IEffectiveAttendanceTimelineResolver
        _timelineResolver;

    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IAttendanceCorrectionAuthorizationPolicy
        _authorizationPolicy;

    private readonly TimeProvider
        _timeProvider;

    public AttendanceCorrectionService(
        IAttendanceRecordRepository recordRepository,
        IAttendanceEventRepository eventRepository,
        IAttendanceCorrectionPersistence correctionPersistence,
        IEffectiveAttendanceTimelineResolver timelineResolver,
        ICurrentUserContext currentUserContext,
        IAttendanceCorrectionAuthorizationPolicy authorizationPolicy,
        TimeProvider timeProvider)
    {
        _recordRepository =
            recordRepository;

        _eventRepository =
            eventRepository;

        _correctionPersistence =
            correctionPersistence;

        _timelineResolver =
            timelineResolver;

        _currentUserContext =
            currentUserContext;

        _authorizationPolicy =
            authorizationPolicy;

        _timeProvider =
            timeProvider;
    }

    public async Task<ApplyAttendanceCorrectionResult> ApplyAsync(
        ApplyAttendanceCorrectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        string? validationError =
            ValidateRequest(
                request);

        if (validationError is not null)
        {
            return Failure(
                validationError);
        }

        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (currentUser is null)
        {
            return Failure(
                "Không thể điều chỉnh chấm công khi chưa có người dùng đăng nhập.");
        }

        AttendanceRecord? record =
            await _recordRepository
                .GetByIdAsync(
                    request.AttendanceRecordId,
                    cancellationToken);

        if (record is null)
        {
            return Failure(
                "Không tìm thấy bản ghi chấm công cần điều chỉnh.");
        }

        bool isAuthorized =
            await _authorizationPolicy
                .CanApplyAsync(
                    new AttendanceCorrectionAuthorizationRequest(
                        currentUser,
                        record.Id,
                        record.EmployeeId,
                        request.Kind),
                    cancellationToken);

        if (!isAuthorized)
        {
            return Failure(
                "Bạn không có quyền điều chỉnh chấm công.");
        }

        IReadOnlyList<AttendanceEvent> rawEvents =
            await _eventRepository
                .GetByAttendanceRecordIdAsync(
                    record.Id,
                    cancellationToken);

        IReadOnlyList<AttendanceCorrection> existingCorrections =
            await _correctionPersistence
                .GetByAttendanceRecordIdAsync(
                    record.Id,
                    cancellationToken);

        IReadOnlyList<EffectiveAttendanceEvent>
            currentTimeline;

        try
        {
            currentTimeline =
                _timelineResolver.Resolve(
                    record.Id,
                    record.EmployeeId,
                    rawEvents,
                    existingCorrections);
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

        int nextRevision =
            existingCorrections.Count + 1;

        Guid affectedEventId;

        AttendanceEventType? beforeEventType =
            null;

        DateTime? beforeOccurredAtUtc =
            null;

        switch (request.Kind)
        {
            case AttendanceCorrectionKind.AddEvent:
                affectedEventId =
                    Guid.NewGuid();

                break;

            case AttendanceCorrectionKind.ChangeEvent:
            case AttendanceCorrectionKind.VoidEvent:
                Guid requestedEventId =
                    request.AffectedEventId!.Value;

                EffectiveAttendanceEvent? target =
                    currentTimeline
                        .FirstOrDefault(
                            item =>
                                item.EventId ==
                                requestedEventId);

                if (target is null)
                {
                    return Failure(
                        "Không tìm thấy sự kiện chấm công cần điều chỉnh trong timeline hiện tại.");
                }

                affectedEventId =
                    target.EventId;

                beforeEventType =
                    target.EventType;

                beforeOccurredAtUtc =
                    target.OccurredAtUtc;

                break;

            default:
                return Failure(
                    "Loại điều chỉnh chấm công không hợp lệ.");
        }

        DateTime correctedAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        AttendanceCorrection correction;

        try
        {
            correction =
                new AttendanceCorrection(
                    Guid.NewGuid(),
                    record.Id,
                    record.EmployeeId,
                    affectedEventId,
                    nextRevision,
                    request.Kind,
                    beforeEventType,
                    beforeOccurredAtUtc,
                    request.AfterEventType,
                    request.AfterOccurredAtUtc,
                    request.Reason,
                    correctedAtUtc,
                    currentUser.UserId,
                    currentUser.Username);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        AttendanceCorrection[] candidateCorrections =
            [
                .. existingCorrections,
                correction
            ];

        try
        {
            IReadOnlyList<EffectiveAttendanceEvent>
                candidateTimeline =
                    _timelineResolver.Resolve(
                        record.Id,
                        record.EmployeeId,
                        rawEvents,
                        candidateCorrections);

            AttendanceEvent[] sequenceEvents =
                candidateTimeline
                    .Select(
                        item =>
                            new AttendanceEvent(
                                item.EventId,
                                record.Id,
                                record.EmployeeId,
                                item.EventType,
                                item.OccurredAtUtc))
                    .ToArray();

            AttendancePunchSequencePolicy
                .EnsureValidTimeline(
                    sequenceEvents);
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

        await _correctionPersistence
            .AppendAsync(
                correction,
                cancellationToken);

        return new ApplyAttendanceCorrectionResult(
            IsSuccessful: true,
            AttendanceCorrectionId:
                correction.Id,
            AttendanceRecordId:
                record.Id,
            AffectedEventId:
                correction.AffectedEventId,
            Revision:
                correction.Revision);
    }

    private static string? ValidateRequest(
        ApplyAttendanceCorrectionRequest request)
    {
        if (request.AttendanceRecordId ==
            Guid.Empty)
        {
            return
                "Mã bản ghi chấm công không hợp lệ.";
        }

        if (!Enum.IsDefined(
                request.Kind))
        {
            return
                "Loại điều chỉnh chấm công không hợp lệ.";
        }

        if (string.IsNullOrWhiteSpace(
                request.Reason))
        {
            return
                "Lý do điều chỉnh chấm công là bắt buộc.";
        }

        if (request.Reason.Trim().Length >
            500)
        {
            return
                "Lý do điều chỉnh không được vượt quá 500 ký tự.";
        }

        bool hasAfterEventType =
            request.AfterEventType.HasValue;

        bool hasAfterOccurredAtUtc =
            request.AfterOccurredAtUtc.HasValue;

        if (hasAfterEventType !=
            hasAfterOccurredAtUtc)
        {
            return
                "Loại sự kiện và thời điểm sau điều chỉnh phải cùng tồn tại hoặc cùng để trống.";
        }

        if (request.AfterEventType.HasValue
            && !Enum.IsDefined(
                request.AfterEventType.Value))
        {
            return
                "Loại sự kiện sau điều chỉnh không hợp lệ.";
        }

        if (request.AfterOccurredAtUtc.HasValue)
        {
            DateTime occurredAtUtc =
                request.AfterOccurredAtUtc.Value;

            if (occurredAtUtc == default
                || occurredAtUtc.Kind !=
                    DateTimeKind.Utc)
            {
                return
                    "Thời điểm sau điều chỉnh phải sử dụng UTC.";
            }
        }

        switch (request.Kind)
        {
            case AttendanceCorrectionKind.AddEvent:
                if (request.AffectedEventId.HasValue)
                {
                    return
                        "Điều chỉnh thêm sự kiện không được chỉ định sự kiện bị tác động.";
                }

                if (!hasAfterEventType)
                {
                    return
                        "Điều chỉnh thêm sự kiện phải có trạng thái sau.";
                }

                break;

            case AttendanceCorrectionKind.ChangeEvent:
                if (!request.AffectedEventId.HasValue
                    || request.AffectedEventId.Value ==
                        Guid.Empty)
                {
                    return
                        "Điều chỉnh sửa sự kiện phải chỉ định sự kiện bị tác động.";
                }

                if (!hasAfterEventType)
                {
                    return
                        "Điều chỉnh sửa sự kiện phải có trạng thái sau.";
                }

                break;

            case AttendanceCorrectionKind.VoidEvent:
                if (!request.AffectedEventId.HasValue
                    || request.AffectedEventId.Value ==
                        Guid.Empty)
                {
                    return
                        "Điều chỉnh hủy sự kiện phải chỉ định sự kiện bị tác động.";
                }

                if (hasAfterEventType)
                {
                    return
                        "Điều chỉnh hủy sự kiện không được có trạng thái sau.";
                }

                break;
        }

        return null;
    }

    private static ApplyAttendanceCorrectionResult Failure(
        string errorMessage)
    {
        return new ApplyAttendanceCorrectionResult(
            IsSuccessful: false,
            ErrorMessage:
                errorMessage);
    }
}
