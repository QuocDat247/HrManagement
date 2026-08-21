using HrManagement.Domain.Attendance.Records;

namespace HrManagement.Domain.Attendance.Calculations;

public static class DailyAttendanceCalculator
{
    public static DailyAttendanceCalculation Calculate(
        AttendanceRecord record,
        IReadOnlyList<AttendanceEvent> events,
            bool hasApprovedLeave = false)
    {
        ArgumentNullException.ThrowIfNull(
            record);

        ArgumentNullException.ThrowIfNull(
            events);

        EnsureOwnership(
            record,
            events);

        AttendancePunchSequencePolicy
            .EnsureValidTimeline(
                events);

        if (events.Count == 0)
        {
            AttendanceCalculationStatus emptyDayStatus =
                !record.IsWorkingDay
                    ? AttendanceCalculationStatus.NonWorkingDay
                    : hasApprovedLeave
                        ? AttendanceCalculationStatus.ApprovedLeave
                        : AttendanceCalculationStatus.Absent;

            return new DailyAttendanceCalculation(
                emptyDayStatus,
                workedMinutes: 0,
                completedPairCount: 0,
                firstClockInAtUtc: null,
                lastClockOutAtUtc: null,
                hasOpenClockIn: false);
        }

        long totalWorkedTicks =
            0;

        int completedPairCount =
            0;

        DateTime? firstClockInAtUtc =
            null;

        DateTime? lastClockOutAtUtc =
            null;

        for (int index = 0;
             index + 1 < events.Count;
             index += 2)
        {
            AttendanceEvent clockIn =
                events[index];

            AttendanceEvent clockOut =
                events[index + 1];

            if (clockIn.EventType !=
                    AttendanceEventType.ClockIn
                || clockOut.EventType !=
                    AttendanceEventType.ClockOut)
            {
                throw new InvalidOperationException(
                    "Lịch sử chấm công không tạo thành các cặp ClockIn và ClockOut hợp lệ.");
            }

            firstClockInAtUtc ??=
                clockIn.OccurredAtUtc;

            totalWorkedTicks +=
                (
                    clockOut.OccurredAtUtc
                    - clockIn.OccurredAtUtc
                ).Ticks;

            completedPairCount++;

            lastClockOutAtUtc =
                clockOut.OccurredAtUtc;
        }

        bool hasOpenClockIn =
            events.Count % 2 != 0;

        if (hasOpenClockIn)
        {
            AttendanceEvent openClockIn =
                events[
                    events.Count - 1];

            if (openClockIn.EventType !=
                AttendanceEventType.ClockIn)
            {
                throw new InvalidOperationException(
                    "Sự kiện chấm công chưa hoàn tất phải là ClockIn.");
            }

            firstClockInAtUtc ??=
                openClockIn.OccurredAtUtc;
        }

        int workedMinutes =
            checked(
                (int)(
                    totalWorkedTicks /
                    TimeSpan.TicksPerMinute));

        AttendanceCalculationStatus status;

        if (hasOpenClockIn)
        {
            status =
                AttendanceCalculationStatus.Incomplete;
        }
        else if (!record.IsWorkingDay)
        {
            status =
                AttendanceCalculationStatus.NonWorkingDay;
        }
        else
        {
            status =
                AttendanceCalculationStatus.Present;
        }

        return new DailyAttendanceCalculation(
            status,
            workedMinutes,
            completedPairCount,
            firstClockInAtUtc,
            lastClockOutAtUtc,
            hasOpenClockIn);
    }

    private static void EnsureOwnership(
        AttendanceRecord record,
        IReadOnlyList<AttendanceEvent> events)
    {
        foreach (AttendanceEvent attendanceEvent
                 in events)
        {
            if (attendanceEvent.AttendanceRecordId !=
                record.Id)
            {
                throw new InvalidOperationException(
                    "Sự kiện chấm công không thuộc bản ghi đang được tính.");
            }

            if (attendanceEvent.EmployeeId !=
                record.EmployeeId)
            {
                throw new InvalidOperationException(
                    "Sự kiện chấm công không thuộc nhân viên đang được tính.");
            }
        }
    }
}
