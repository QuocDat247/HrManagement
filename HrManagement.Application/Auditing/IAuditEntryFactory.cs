using HrManagement.Domain.Auditing;

namespace HrManagement.Application.Auditing;

public interface IAuditEntryFactory
{
    AuditEntry Create(
        AuditAction action,
        string entityType,
        Guid entityId,
        Guid? employeeId = null);
}
