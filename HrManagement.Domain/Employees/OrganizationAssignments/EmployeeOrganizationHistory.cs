namespace HrManagement.Domain.Employees.OrganizationAssignments;

public sealed class EmployeeOrganizationHistory
{
    private readonly List<EmployeeOrganizationAssignment>
        _assignments;

    public Guid EmployeeId
    {
        get;
    }

    public IReadOnlyList<EmployeeOrganizationAssignment>
        Assignments =>
            _assignments;

    public EmployeeOrganizationAssignment?
        CurrentAssignment =>
            _assignments.LastOrDefault(
                assignment =>
                    assignment.IsOpen);

    public EmployeeOrganizationAssignment?
        LatestAssignment =>
            _assignments.LastOrDefault();

    public EmployeeOrganizationHistory(
        Guid employeeId,
        IEnumerable<EmployeeOrganizationAssignment>
            assignments)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        ArgumentNullException.ThrowIfNull(
            assignments);

        List<EmployeeOrganizationAssignment>
            orderedAssignments =
                assignments
                    .OrderBy(
                        assignment =>
                            assignment.StartDate)
                    .ThenBy(
                        assignment =>
                            assignment.Id)
                    .ToList();

        ValidateEmployeeOwnership(
            employeeId,
            orderedAssignments);

        ValidateDuplicateAssignmentIds(
            orderedAssignments);

        ValidateAssignmentsDoNotOverlap(
            orderedAssignments);

        EmployeeId =
            employeeId;

        _assignments =
            orderedAssignments;
    }

    public EmployeeOrganizationAssignment Transfer(
        Guid assignmentId,
        Guid departmentId,
        string departmentCode,
        string departmentName,
        Guid positionId,
        string positionCode,
        string positionName,
        DateOnly effectiveDate)
    {
        EmployeeOrganizationAssignment current =
            CurrentAssignment
            ?? throw new InvalidOperationException(
                "Nhân viên không có phân công hiện tại.");

        if (effectiveDate == default)
        {
            throw new ArgumentException(
                "Ngày điều chuyển không hợp lệ.",
                nameof(effectiveDate));
        }

        if (effectiveDate <= current.StartDate)
        {
            throw new ArgumentException(
                "Ngày điều chuyển phải sau ngày bắt đầu "
                + "của phân công hiện tại.",
                nameof(effectiveDate));
        }

        bool departmentUnchanged =
            current.DepartmentId ==
            departmentId;

        bool positionUnchanged =
            current.PositionId ==
            positionId;

        if (departmentUnchanged
            && positionUnchanged)
        {
            throw new InvalidOperationException(
                "Điều chuyển phải thay đổi phòng ban, "
                + "chức danh hoặc cả hai.");
        }

        current.Close(
            effectiveDate.AddDays(-1));

        var newAssignment =
            new EmployeeOrganizationAssignment(
                assignmentId,
                EmployeeId,
                current.EmploymentPeriodId,
                departmentId,
                departmentCode,
                departmentName,
                positionId,
                positionCode,
                positionName,
                effectiveDate);

        _assignments.Add(
            newAssignment);

        return newAssignment;
    }

    public EmployeeOrganizationAssignment
        CloseCurrentAssignment(
            DateOnly endDate)
    {
        EmployeeOrganizationAssignment current =
            CurrentAssignment
            ?? throw new InvalidOperationException(
                "Nhân viên không có phân công hiện tại.");

        current.Close(
            endDate);

        return current;
    }

    public EmployeeOrganizationAssignment
        ReopenLatestAssignment(
            DateOnly expectedEndDate)
    {
        if (CurrentAssignment is not null)
        {
            throw new InvalidOperationException(
                "Nhân viên đã có phân công đang mở.");
        }

        EmployeeOrganizationAssignment latest =
            LatestAssignment
            ?? throw new InvalidOperationException(
                "Nhân viên chưa có lịch sử phân công.");

        if (latest.EndDate
            != expectedEndDate)
        {
            throw new InvalidOperationException(
                "Ngày kết thúc của lịch sử phân công "
                + "không khớp.");
        }

        latest.Reopen();

        return latest;
    }

    public EmployeeOrganizationAssignment
        StartNewAssignment(
            Guid assignmentId,
            Guid employmentPeriodId,
            Guid departmentId,
            string departmentCode,
            string departmentName,
            Guid positionId,
            string positionCode,
            string positionName,
            DateOnly startDate)
    {
        if (CurrentAssignment is not null)
        {
            throw new InvalidOperationException(
                "Nhân viên đã có phân công đang mở.");
        }

        EmployeeOrganizationAssignment latest =
            LatestAssignment
            ?? throw new InvalidOperationException(
                "Nhân viên chưa có lịch sử phân công.");

        if (!latest.EndDate.HasValue)
        {
            throw new InvalidOperationException(
                "Phân công gần nhất chưa được kết thúc.");
        }

        if (startDate <= latest.EndDate.Value)
        {
            throw new ArgumentException(
                "Ngày bắt đầu phân công mới phải sau "
                + "ngày kết thúc phân công gần nhất.",
                nameof(startDate));
        }

        if (employmentPeriodId
            == latest.EmploymentPeriodId)
        {
            throw new InvalidOperationException(
                "Phân công sau khi tái tuyển dụng phải "
                + "thuộc giai đoạn làm việc mới.");
        }

        var newAssignment =
            new EmployeeOrganizationAssignment(
                assignmentId,
                EmployeeId,
                employmentPeriodId,
                departmentId,
                departmentCode,
                departmentName,
                positionId,
                positionCode,
                positionName,
                startDate);

        _assignments.Add(
            newAssignment);

        return newAssignment;
    }

    private static void ValidateEmployeeOwnership(
        Guid employeeId,
        IReadOnlyList<EmployeeOrganizationAssignment>
            assignments)
    {
        if (assignments.Any(
                assignment =>
                    assignment.EmployeeId
                    != employeeId))
        {
            throw new ArgumentException(
                "Lịch sử phân công chứa dữ liệu "
                + "thuộc nhân viên khác.",
                nameof(assignments));
        }
    }

    private static void ValidateDuplicateAssignmentIds(
        IReadOnlyList<EmployeeOrganizationAssignment>
            assignments)
    {
        bool hasDuplicateIds =
            assignments
                .GroupBy(
                    assignment =>
                        assignment.Id)
                .Any(
                    group =>
                        group.Count() > 1);

        if (hasDuplicateIds)
        {
            throw new ArgumentException(
                "Lịch sử phân công chứa bản ghi bị trùng.",
                nameof(assignments));
        }
    }

    private static void ValidateAssignmentsDoNotOverlap(
        IReadOnlyList<EmployeeOrganizationAssignment>
            assignments)
    {
        for (int index = 1;
             index < assignments.Count;
             index++)
        {
            EmployeeOrganizationAssignment previous =
                assignments[index - 1];

            EmployeeOrganizationAssignment current =
                assignments[index];

            if (previous.EndDate is null)
            {
                throw new ArgumentException(
                    "Không thể có nhiều hơn một "
                    + "phân công đang mở.",
                    nameof(assignments));
            }

            if (current.StartDate
                <= previous.EndDate.Value)
            {
                throw new ArgumentException(
                    "Các phân công không được chồng lấn.",
                    nameof(assignments));
            }
        }
    }
}
