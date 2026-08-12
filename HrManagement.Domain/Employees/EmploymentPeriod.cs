namespace HrManagement.Domain.Employees;

public sealed class EmploymentPeriod
{
    public Guid Id { get; }

    public Guid EmployeeId { get; }

    public DateOnly StartDate { get; }

    public DateOnly? EndDate { get; private set; }

    public bool IsOpen =>
        EndDate is null;

    public void Close(DateOnly endDate)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException(
                "Giai đoạn làm việc đã được kết thúc.");
        }

        if (endDate == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc không hợp lệ.",
                nameof(endDate));
        }

        if (endDate < StartDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc không thể trước ngày bắt đầu.",
                nameof(endDate));
        }

        EndDate = endDate;
    }

    public EmploymentPeriod(
        Guid id,
        Guid employeeId,
        DateOnly startDate,
        DateOnly? endDate = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giai đoạn làm việc không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (startDate == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu làm việc không hợp lệ.",
                nameof(startDate));
        }

        if (endDate.HasValue
            && endDate.Value < startDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc không thể trước ngày bắt đầu.",
                nameof(endDate));
        }

        Id = id;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
    }

    internal void Reopen()
    {
        if (IsOpen)
        {
            throw new InvalidOperationException(
                "Giai đoạn làm việc đang mở.");
        }

        EndDate = null;
    }
}
