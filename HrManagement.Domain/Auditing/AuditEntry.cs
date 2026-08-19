namespace HrManagement.Domain.Auditing;

public sealed class AuditEntry
{
    public Guid Id
    {
        get;
    }

    public DateTime OccurredAtUtc
    {
        get;
    }

    public string ActorUserId
    {
        get;
    }

    public string ActorUsername
    {
        get;
    }

    public AuditAction Action
    {
        get;
    }

    public string EntityType
    {
        get;
    }

    public Guid EntityId
    {
        get;
    }

    public Guid? EmployeeId
    {
        get;
    }

    public AuditEntry(
        Guid id,
        DateTime occurredAtUtc,
        string actorUserId,
        string actorUsername,
        AuditAction action,
        string entityType,
        Guid entityId,
        Guid? employeeId = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã audit không hợp lệ.",
                nameof(id));
        }

        if (occurredAtUtc.Kind
            != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Thời điểm audit phải sử dụng UTC.",
                nameof(occurredAtUtc));
        }

        if (string.IsNullOrWhiteSpace(
                actorUserId))
        {
            throw new ArgumentException(
                "Actor user id là bắt buộc.",
                nameof(actorUserId));
        }

        if (string.IsNullOrWhiteSpace(
                actorUsername))
        {
            throw new ArgumentException(
                "Actor username là bắt buộc.",
                nameof(actorUsername));
        }

        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(
                nameof(action));
        }

        if (string.IsNullOrWhiteSpace(
                entityType))
        {
            throw new ArgumentException(
                "Loại đối tượng audit là bắt buộc.",
                nameof(entityType));
        }

        if (entityId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã đối tượng audit không hợp lệ.",
                nameof(entityId));
        }

        if (employeeId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã nhân viên không hợp lệ.",
                nameof(employeeId));
        }

        Id =
            id;

        OccurredAtUtc =
            occurredAtUtc;

        ActorUserId =
            actorUserId.Trim();

        ActorUsername =
            actorUsername.Trim();

        Action =
            action;

        EntityType =
            entityType.Trim();

        EntityId =
            entityId;

        EmployeeId =
            employeeId;
    }
}
