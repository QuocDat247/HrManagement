using System.Data;
using HrManagement.Application.Leave.Requests;
using HrManagement.Domain.Leave.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Leave.Requests;

public sealed class EfLeaveRequestStatusTransitionPersistence
    : ILeaveRequestStatusTransitionPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfLeaveRequestStatusTransitionPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task ApplyAsync(
        LeaveRequestStatusChange statusChange,
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

        int affectedRows =
            await dbContext
                .LeaveRequests
                .Where(
                    request =>
                        request.Id ==
                            statusChange.LeaveRequestId
                        && request.Status ==
                            statusChange.FromStatus)
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(
                            request =>
                                request.Status,
                            statusChange.ToStatus),
                    cancellationToken);

        if (affectedRows == 0)
        {
            bool requestExists =
                await dbContext
                    .LeaveRequests
                    .AsNoTracking()
                    .AnyAsync(
                        request =>
                            request.Id ==
                            statusChange.LeaveRequestId,
                        cancellationToken);

            if (!requestExists)
            {
                throw new DbUpdateConcurrencyException(
                    "Không còn tìm thấy đơn nghỉ phép.");
            }

            throw new DbUpdateConcurrencyException(
                "Trạng thái đơn nghỉ phép đã thay đổi trước khi lưu.");
        }

        bool historyAlreadyExists =
            await dbContext
                .LeaveRequestStatusChanges
                .AsNoTracking()
                .AnyAsync(
                    existing =>
                        existing.Id ==
                        statusChange.Id,
                    cancellationToken);

        if (historyAlreadyExists)
        {
            throw new DbUpdateConcurrencyException(
                "Lịch sử thay đổi trạng thái này đã tồn tại.");
        }

        await dbContext
            .LeaveRequestStatusChanges
            .AddAsync(
                statusChange,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new DbUpdateConcurrencyException(
                "Không thể lưu lịch sử thay đổi trạng thái do dữ liệu đã thay đổi.",
                exception);
        }

        await transaction.CommitAsync(
            cancellationToken);
    }
}
