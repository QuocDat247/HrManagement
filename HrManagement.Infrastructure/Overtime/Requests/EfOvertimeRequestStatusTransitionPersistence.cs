using System.Data;
using HrManagement.Application.Auditing;
using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Overtime.Requests;

public sealed class EfOvertimeRequestStatusTransitionPersistence
    : IOvertimeRequestStatusTransitionPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IAuditEntryFactory
        _auditEntryFactory;

    public EfOvertimeRequestStatusTransitionPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task ApplyAsync(
        OvertimeRequestStatusChange statusChange,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            statusChange);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        OvertimeRequest? persistedRequest =
            await dbContext
                .OvertimeRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    request =>
                        request.Id ==
                        statusChange.OvertimeRequestId,
                    cancellationToken);

        if (persistedRequest is null)
        {
            throw new OvertimeRequestStatusConcurrencyException(
                "Không còn tìm thấy yêu cầu tăng ca.");
        }

        if (persistedRequest.Status !=
            statusChange.PreviousStatus)
        {
            throw new OvertimeRequestStatusConcurrencyException(
                "Yêu cầu tăng ca đã thay đổi trạng thái. Vui lòng làm mới dữ liệu trước khi thao tác.");
        }

        if (!string.Equals(
                statusChange.ChangedByUserId,
                actorUserId,
                StringComparison.Ordinal)
            || !string.Equals(
                statusChange.ChangedByUsername,
                actorUsername,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Người thay đổi trạng thái tăng ca không khớp với actor của transition.");
        }

        OvertimeRequestStatusChange persistedStatusChange;

        try
        {
            persistedStatusChange =
                persistedRequest.TransitionTo(
                    statusChange.Id,
                    statusChange.NewStatus,
                    statusChange.ChangedAtUtc,
                    statusChange.ChangedByUserId,
                    statusChange.ChangedByUsername,
                    statusChange.ApprovedMinutes,
                    statusChange.Note);
        }
        catch (ArgumentException exception)
        {
            throw new OvertimeRequestStatusConcurrencyException(
                "Điều kiện thay đổi trạng thái tăng ca đã thay đổi trước khi lưu.",
                exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new OvertimeRequestStatusConcurrencyException(
                "Điều kiện thay đổi trạng thái tăng ca đã thay đổi trước khi lưu.",
                exception);
        }

        AuditEntry auditEntry =
            _auditEntryFactory.Create(
                AuditAction.Updated,
                AuditEntityTypes.OvertimeRequest,
                persistedRequest.Id,
                persistedRequest.EmployeeId);

        if (!string.Equals(
                auditEntry.ActorUserId,
                actorUserId,
                StringComparison.Ordinal)
            || !string.Equals(
                auditEntry.ActorUsername,
                actorUsername,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Người thay đổi trạng thái tăng ca không khớp với người dùng audit hiện tại.");
        }

        int affectedRows =
            await dbContext
                .OvertimeRequests
                .Where(
                    request =>
                        request.Id ==
                            persistedRequest.Id
                        && request.Status ==
                            statusChange.PreviousStatus)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                request =>
                                    request.Status,
                                persistedStatusChange.NewStatus)
                            .SetProperty(
                                request =>
                                    request.ApprovedMinutes,
                                persistedStatusChange.NewStatus ==
                                    OvertimeRequestStatus.Approved
                                    ? persistedStatusChange.ApprovedMinutes
                                    : null),
                    cancellationToken);

        if (affectedRows != 1)
        {
            throw new OvertimeRequestStatusConcurrencyException(
                "Yêu cầu tăng ca đã thay đổi trạng thái trước khi lưu. Vui lòng làm mới dữ liệu.");
        }

        await dbContext
            .OvertimeRequestStatusChanges
            .AddAsync(
                persistedStatusChange,
                cancellationToken);

        await dbContext
            .AuditEntries
            .AddAsync(
                auditEntry,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new OvertimeRequestStatusConcurrencyException(
                "Không thể lưu thay đổi trạng thái tăng ca do dữ liệu đã thay đổi.",
                exception);
        }

        await transaction.CommitAsync(
            cancellationToken);
    }
}
