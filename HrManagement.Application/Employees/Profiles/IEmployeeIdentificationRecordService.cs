namespace HrManagement.Application.Employees.Profiles;

public interface IEmployeeIdentificationRecordService
{
    Task<IReadOnlyList<EmployeeIdentificationRecordDetails>>
        GetRecordsAsync(
            Guid employeeId,
            CancellationToken cancellationToken = default);

    Task<EmployeeIdentificationRecordOperationResult>
        SaveRecordAsync(
            SaveEmployeeIdentificationRecordRequest request,
            CancellationToken cancellationToken = default);

    Task<EmployeeIdentificationRecordOperationResult>
        DeleteRecordAsync(
            Guid employeeId,
            Guid recordId,
            CancellationToken cancellationToken = default);
}
