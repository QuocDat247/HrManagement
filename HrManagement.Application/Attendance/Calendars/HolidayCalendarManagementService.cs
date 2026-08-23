using HrManagement.Domain.Attendance.Calendars;

namespace HrManagement.Application.Attendance.Calendars;

public sealed class HolidayCalendarManagementService
    : IHolidayCalendarManagementService
{
    private readonly IHolidayCalendarManagementPersistence
        _persistence;

    public HolidayCalendarManagementService(
        IHolidayCalendarManagementPersistence persistence)
    {
        _persistence =
            persistence;
    }

    public async Task<HolidayCalendarManagementResult> CreateAsync(
        CreateHolidayCalendarDayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        HolidayCalendarDay holiday;

        try
        {
            holiday =
                new HolidayCalendarDay(
                    Guid.NewGuid(),
                    request.Date,
                    request.Name);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        HolidayCalendarDay? existing =
            await _persistence
                .GetByDateAsync(
                    holiday.Date,
                    cancellationToken);

        if (existing is not null)
        {
            return Failure(
                "Ngày này đã tồn tại trong lịch ngày lễ.");
        }

        await _persistence
            .CreateAsync(
                holiday,
                cancellationToken);

        return Success(
            holiday.Id);
    }

    public async Task<HolidayCalendarManagementResult> RenameAsync(
        RenameHolidayCalendarDayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.HolidayCalendarDayId ==
            Guid.Empty)
        {
            return Failure(
                "Mã ngày lễ không hợp lệ.");
        }

        HolidayCalendarDay? holiday =
            await _persistence
                .GetByIdAsync(
                    request.HolidayCalendarDayId,
                    cancellationToken);

        if (holiday is null)
        {
            return Failure(
                "Không tìm thấy ngày lễ.");
        }

        try
        {
            holiday.Rename(
                request.Name);
        }
        catch (ArgumentException exception)
        {
            return Failure(
                exception.Message);
        }

        await _persistence
            .UpdateAsync(
                holiday,
                cancellationToken);

        return Success(
            holiday.Id);
    }

    public async Task<HolidayCalendarManagementResult> DeactivateAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default)
    {
        if (holidayCalendarDayId ==
            Guid.Empty)
        {
            return Failure(
                "Mã ngày lễ không hợp lệ.");
        }

        HolidayCalendarDay? holiday =
            await _persistence
                .GetByIdAsync(
                    holidayCalendarDayId,
                    cancellationToken);

        if (holiday is null)
        {
            return Failure(
                "Không tìm thấy ngày lễ.");
        }

        if (!holiday.IsActive)
        {
            return Success(
                holiday.Id);
        }

        holiday.Deactivate();

        await _persistence
            .UpdateAsync(
                holiday,
                cancellationToken);

        return Success(
            holiday.Id);
    }

    public async Task<HolidayCalendarManagementResult> ReactivateAsync(
        Guid holidayCalendarDayId,
        CancellationToken cancellationToken = default)
    {
        if (holidayCalendarDayId ==
            Guid.Empty)
        {
            return Failure(
                "Mã ngày lễ không hợp lệ.");
        }

        HolidayCalendarDay? holiday =
            await _persistence
                .GetByIdAsync(
                    holidayCalendarDayId,
                    cancellationToken);

        if (holiday is null)
        {
            return Failure(
                "Không tìm thấy ngày lễ.");
        }

        if (holiday.IsActive)
        {
            return Success(
                holiday.Id);
        }

        holiday.Reactivate();

        await _persistence
            .UpdateAsync(
                holiday,
                cancellationToken);

        return Success(
            holiday.Id);
    }

    private static HolidayCalendarManagementResult Success(
        Guid holidayCalendarDayId)
    {
        return new HolidayCalendarManagementResult(
            true,
            holidayCalendarDayId);
    }

    private static HolidayCalendarManagementResult Failure(
        string errorMessage)
    {
        return new HolidayCalendarManagementResult(
            false,
            null,
            errorMessage);
    }
}
