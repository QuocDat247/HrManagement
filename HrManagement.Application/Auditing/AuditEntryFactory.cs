using HrManagement.Application.Authentication;
using HrManagement.Domain.Auditing;

namespace HrManagement.Application.Auditing;

public sealed class AuditEntryFactory
    : IAuditEntryFactory
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly TimeProvider
        _timeProvider;

    public AuditEntryFactory(
        ICurrentUserContext currentUserContext,
        TimeProvider timeProvider)
    {
        _currentUserContext =
            currentUserContext;

        _timeProvider =
            timeProvider;
    }

    public AuditEntry Create(
        AuditAction action,
        string entityType,
        Guid entityId,
        Guid? employeeId = null)
    {
        AuthenticatedUser? currentUser =
            _currentUserContext.CurrentUser;

        if (currentUser is null)
        {
            throw new InvalidOperationException(
                "Không thể tạo audit khi chưa có người dùng đăng nhập.");
        }

        DateTime occurredAtUtc =
            _timeProvider
                .GetUtcNow()
                .UtcDateTime;

        return new AuditEntry(
            Guid.NewGuid(),
            occurredAtUtc,
            currentUser.UserId,
            currentUser.Username,
            action,
            entityType,
            entityId,
            employeeId);
    }
}
