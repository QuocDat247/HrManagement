using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Application.Employees.Profiles;

public sealed class EmployeeIdentificationRecordService
    : IEmployeeIdentificationRecordService
{
    private readonly IEmployeeRepository
        _employeeRepository;

    private readonly IEmployeeIdentificationRecordRepository
        _recordRepository;

    public EmployeeIdentificationRecordService(
        IEmployeeRepository employeeRepository,
        IEmployeeIdentificationRecordRepository recordRepository)
    {
        _employeeRepository =
            employeeRepository;

        _recordRepository =
            recordRepository;
    }

    public async Task<IReadOnlyList<EmployeeIdentificationRecordDetails>>
        GetRecordsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        var employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            throw new KeyNotFoundException(
                "Nhân viên không tồn tại.");
        }

        IReadOnlyList<EmployeeIdentificationRecord> records =
            await _recordRepository
                .GetByEmployeeIdAsync(
                    employeeId,
                    cancellationToken);

        return records
            .Select(Map)
            .ToList();
    }

    public async Task<EmployeeIdentificationRecordOperationResult>
        SaveRecordAsync(
            SaveEmployeeIdentificationRecordRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.EmployeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (request.RecordId == Guid.Empty)
        {
            return Failure(
                "Mã giấy tờ không hợp lệ.");
        }

        if (!Enum.IsDefined(
                request.Type))
        {
            return Failure(
                "Loại giấy tờ không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(
                request.DocumentNumber))
        {
            return Failure(
                "Số giấy tờ là bắt buộc.");
        }

        if (request.IssueDate.HasValue
            && request.ExpiryDate.HasValue
            && request.ExpiryDate.Value
                < request.IssueDate.Value)
        {
            return Failure(
                "Ngày hết hạn không được trước ngày cấp.");
        }

        var employee =
            await _employeeRepository
                .GetByIdAsync(
                    request.EmployeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Nhân viên không tồn tại.");
        }

        Guid recordId;

        if (request.RecordId.HasValue)
        {
            IReadOnlyList<EmployeeIdentificationRecord> existingRecords =
                await _recordRepository
                    .GetByEmployeeIdAsync(
                        request.EmployeeId,
                        cancellationToken);

            bool recordExists =
                existingRecords.Any(
                    record =>
                        record.Id ==
                        request.RecordId.Value);

            if (!recordExists)
            {
                return Failure(
                    "Giấy tờ định danh không tồn tại.");
            }

            recordId =
                request.RecordId.Value;
        }
        else
        {
            recordId =
                Guid.NewGuid();
        }

        var record =
            new EmployeeIdentificationRecord(
                recordId,
                request.EmployeeId,
                request.Type,
                request.DocumentNumber,
                request.IssueDate,
                request.ExpiryDate,
                request.IssuingAuthority,
                request.PlaceOfIssue,
                request.IssuingCountry);

        await _recordRepository
            .UpsertAsync(
                record,
                cancellationToken);

        return new EmployeeIdentificationRecordOperationResult(
            IsSuccessful: true,
            RecordId: recordId);
    }

    public async Task<EmployeeIdentificationRecordOperationResult>
        DeleteRecordAsync(
            Guid employeeId,
            Guid recordId,
            CancellationToken cancellationToken = default)
    {
        if (employeeId == Guid.Empty)
        {
            return Failure(
                "Mã nhân viên không hợp lệ.");
        }

        if (recordId == Guid.Empty)
        {
            return Failure(
                "Mã giấy tờ không hợp lệ.");
        }

        var employee =
            await _employeeRepository
                .GetByIdAsync(
                    employeeId,
                    cancellationToken);

        if (employee is null)
        {
            return Failure(
                "Nhân viên không tồn tại.");
        }

        await _recordRepository
            .DeleteAsync(
                employeeId,
                recordId,
                cancellationToken);

        return new EmployeeIdentificationRecordOperationResult(
            IsSuccessful: true);
    }

    private static EmployeeIdentificationRecordDetails Map(
        EmployeeIdentificationRecord record)
    {
        return new EmployeeIdentificationRecordDetails(
            record.Id,
            record.Type,
            record.DocumentNumber,
            record.IssueDate,
            record.ExpiryDate,
            record.IssuingAuthority,
            record.PlaceOfIssue,
            record.IssuingCountry);
    }

    private static EmployeeIdentificationRecordOperationResult Failure(
        string errorMessage)
    {
        return new EmployeeIdentificationRecordOperationResult(
            IsSuccessful: false,
            ErrorMessage: errorMessage);
    }
}
