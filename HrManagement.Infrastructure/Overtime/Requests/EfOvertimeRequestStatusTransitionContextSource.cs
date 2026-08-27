using HrManagement.Application.Overtime.Requests;
using HrManagement.Domain.Overtime.Requests;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Overtime.Requests;

public sealed class EfOvertimeRequestStatusTransitionContextSource
    : IOvertimeRequestStatusTransitionContextSource
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    public EfOvertimeRequestStatusTransitionContextSource(
        IDbContextFactory<HrManagementDbContext> dbContextFactory)
    {
        _dbContextFactory =
            dbContextFactory;
    }

    public async Task<OvertimeRequest?> GetByIdAsync(
        Guid overtimeRequestId,
        CancellationToken cancellationToken = default)
    {
        if (overtimeRequestId == Guid.Empty)
        {
            throw new ArgumentException(
                "Mã yêu cầu tăng ca không hợp lệ.",
                nameof(overtimeRequestId));
        }

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        return await dbContext
            .OvertimeRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(
                request =>
                    request.Id ==
                    overtimeRequestId,
                cancellationToken);
    }
}
