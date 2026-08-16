namespace HrManagement.Domain.Employees.OrganizationAssignments;

public sealed class EmployeeOrganizationAssignment
{
    public Guid Id
    {
        get;
    }

    public Guid EmployeeId
    {
        get;
    }

    public Guid EmploymentPeriodId
    {
        get;
    }

    public Guid DepartmentId
    {
        get;
    }

    public string DepartmentCode
    {
        get;
    }

    public string DepartmentName
    {
        get;
    }

    public Guid PositionId
    {
        get;
    }

    public string PositionCode
    {
        get;
    }

    public string PositionName
    {
        get;
    }

    public DateOnly StartDate
    {
        get;
    }

    public DateOnly? EndDate
    {
        get;
        private set;
    }

    public bool IsBaseline
    {
        get;
    }

    public bool IsOpen =>
        EndDate is null;

    public EmployeeOrganizationAssignment(
        Guid id,
        Guid employeeId,
        Guid employmentPeriodId,
        Guid departmentId,
        string departmentCode,
        string departmentName,
        Guid positionId,
        string positionCode,
        string positionName,
        DateOnly startDate,
        DateOnly? endDate = null,
        bool isBaseline = false)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã phân công không hợp lệ.",
                nameof(id));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        if (employmentPeriodId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã giai đoạn làm việc không hợp lệ.",
                nameof(employmentPeriodId));
        }

        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã phòng ban không hợp lệ.",
                nameof(departmentId));
        }

        if (string.IsNullOrWhiteSpace(
                departmentCode))
        {
            throw new ArgumentException(
                "Mã phòng ban không được để trống.",
                nameof(departmentCode));
        }

        if (string.IsNullOrWhiteSpace(
                departmentName))
        {
            throw new ArgumentException(
                "Tên phòng ban không được để trống.",
                nameof(departmentName));
        }

        if (positionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã chức danh không hợp lệ.",
                nameof(positionId));
        }

        if (string.IsNullOrWhiteSpace(
                positionCode))
        {
            throw new ArgumentException(
                "Mã chức danh không được để trống.",
                nameof(positionCode));
        }

        if (string.IsNullOrWhiteSpace(
                positionName))
        {
            throw new ArgumentException(
                "Tên chức danh không được để trống.",
                nameof(positionName));
        }

        if (startDate == default)
        {
            throw new ArgumentException(
                "Ngày bắt đầu phân công không hợp lệ.",
                nameof(startDate));
        }

        if (endDate.HasValue
            && endDate.Value < startDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc phân công không thể "
                + "trước ngày bắt đầu.",
                nameof(endDate));
        }

        Id = id;
        EmployeeId = employeeId;
        EmploymentPeriodId = employmentPeriodId;

        DepartmentId = departmentId;
        DepartmentCode = departmentCode.Trim();
        DepartmentName = departmentName.Trim();

        PositionId = positionId;
        PositionCode = positionCode.Trim();
        PositionName = positionName.Trim();

        StartDate = startDate;
        EndDate = endDate;

        IsBaseline =isBaseline;
    }

    public void Close(
        DateOnly endDate)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException(
                "Phân công đã được kết thúc.");
        }

        if (endDate == default)
        {
            throw new ArgumentException(
                "Ngày kết thúc phân công không hợp lệ.",
                nameof(endDate));
        }

        if (endDate < StartDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc phân công không thể "
                + "trước ngày bắt đầu.",
                nameof(endDate));
        }

        EndDate = endDate;
    }

    internal void Reopen()
    {
        if (IsOpen)
        {
            throw new InvalidOperationException(
                "Phân công đang mở.");
        }

        EndDate = null;
    }
}
