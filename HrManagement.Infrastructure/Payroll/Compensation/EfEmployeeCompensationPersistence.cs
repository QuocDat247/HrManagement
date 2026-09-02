using System.Data;
using HrManagement.Application.Auditing;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Domain.Payroll.Periods;
using HrManagement.Domain.Auditing;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Payroll.Compensation;
using HrManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Payroll.Compensation;

public sealed class EfEmployeeCompensationPersistence
    : IEmployeeCompensationPersistence
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IAuditEntryFactory
        _auditEntryFactory;

    public EfEmployeeCompensationPersistence(
        IDbContextFactory<HrManagementDbContext> dbContextFactory,
        IAuditEntryFactory auditEntryFactory)
    {
        _dbContextFactory =
            dbContextFactory;

        _auditEntryFactory =
            auditEntryFactory;
    }

    public async Task ApplyAsync(
        EmployeeCompensation? closedCompensation,
        EmployeeCompensation newCompensation,
        string actorUserId,
        string actorUsername,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            newCompensation);

        ValidateInput(
            closedCompensation,
            newCompensation,
            actorUserId,
            actorUsername);

        await using HrManagementDbContext dbContext =
            await _dbContextFactory
                .CreateDbContextAsync(
                    cancellationToken);

        await using var transaction =
            await dbContext.Database
                .BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

        bool employeeExists =
            await dbContext
                .Employees
                .AsNoTracking()
                .AnyAsync(
                    employee =>
                        employee.Id ==
                            newCompensation.EmployeeId,
                    cancellationToken);

        if (!employeeExists)
        {
            throw new EmployeeCompensationConcurrencyException(
                "Nhân viên không còn tồn tại.");
        }

        EmploymentPeriod? employmentPeriod =
            await dbContext
                .EmploymentPeriods
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    period =>
                        period.Id ==
                            newCompensation.EmploymentPeriodId,
                    cancellationToken);

        if (employmentPeriod is null)
        {
            throw new EmployeeCompensationConcurrencyException(
                "Giai đoạn làm việc không còn tồn tại.");
        }

        if (employmentPeriod.EmployeeId !=
            newCompensation.EmployeeId)
        {
            throw new EmployeeCompensationConcurrencyException(
                "Giai đoạn làm việc không thuộc nhân viên.");
        }

        if (newCompensation.EffectiveFrom <
                employmentPeriod.StartDate
            || (
                employmentPeriod.EndDate.HasValue
                && newCompensation.EffectiveFrom >
                    employmentPeriod.EndDate.Value
            ))
        {
            throw new EmployeeCompensationConcurrencyException(
                "Ngày hiệu lực lương không còn nằm trong giai đoạn làm việc của nhân viên.");
        }

        int fromPeriodKey =
            newCompensation.EffectiveFrom.Year
            * 100
            + newCompensation.EffectiveFrom.Month;

                int? toPeriodKey =
                    employmentPeriod.EndDate.HasValue
                        ? employmentPeriod.EndDate.Value.Year
                            * 100
                            + employmentPeriod.EndDate.Value.Month
                        : null;

        bool overlapsClosedPayrollPeriod =
            await dbContext
                .PayrollPeriods
                .AsNoTracking()
                .AnyAsync(
                    period =>
                        period.Status ==
                            PayrollPeriodStatus.Closed
                        && (
                            period.Year * 100
                            + period.Month
                        ) >= fromPeriodKey
                        && (
                            !toPeriodKey.HasValue
                            || (
                                period.Year * 100
                                + period.Month
                            ) <= toPeriodKey.Value
                        ),
                    cancellationToken);

        if (overlapsClosedPayrollPeriod)
        {
            throw new InvalidOperationException(
                "Không thể thay đổi cấu hình lương vì khoảng hiệu lực mới chồng lấn kỳ lương đã đóng.");
        }

        EmployeeCompensation[] currentRows =
            await dbContext
                .EmployeeCompensations
                .AsNoTracking()
                .Where(
                    compensation =>
                        compensation.EmploymentPeriodId ==
                            employmentPeriod.Id
                        && !compensation.EffectiveTo.HasValue)
                .OrderBy(
                    compensation =>
                        compensation.EffectiveFrom)
                .ThenBy(
                    compensation =>
                        compensation.Id)
                .Take(
                    2)
                .ToArrayAsync(
                    cancellationToken);

        if (currentRows.Length > 1)
        {
            throw new EmployeeCompensationConcurrencyException(
                "Có nhiều cấu hình lương đang mở. Dữ liệu cần được kiểm tra trước khi tiếp tục.");
        }

        EmployeeCompensation? persistedCurrent =
            currentRows.SingleOrDefault();

        if (closedCompensation is null)
        {
            if (persistedCurrent is not null)
            {
                throw new EmployeeCompensationConcurrencyException(
                    "Cấu hình lương đã thay đổi. Vui lòng làm mới dữ liệu trước khi thao tác.");
            }
        }
        else
        {
            if (persistedCurrent is null
                || persistedCurrent.Id !=
                    closedCompensation.Id)
            {
                throw new EmployeeCompensationConcurrencyException(
                    "Cấu hình lương hiện tại đã thay đổi. Vui lòng làm mới dữ liệu trước khi thao tác.");
            }

            if (persistedCurrent.EmployeeId !=
                    closedCompensation.EmployeeId
                || persistedCurrent.EmploymentPeriodId !=
                    closedCompensation.EmploymentPeriodId
                || persistedCurrent.EffectiveFrom !=
                    closedCompensation.EffectiveFrom
                || persistedCurrent.MonthlyBaseSalary !=
                    closedCompensation.MonthlyBaseSalary
                || !string.Equals(
                    persistedCurrent.CurrencyCode,
                    closedCompensation.CurrencyCode,
                    StringComparison.Ordinal))
            {
                throw new EmployeeCompensationConcurrencyException(
                    "Dữ liệu cấu hình lương hiện tại đã thay đổi trước khi lưu.");
            }
        }

        Guid? expectedCurrentId =
            persistedCurrent?.Id;

        bool overlappingCompensationExists =
            await dbContext
                .EmployeeCompensations
                .AsNoTracking()
                .AnyAsync(
                    compensation =>
                        compensation.EmploymentPeriodId ==
                            newCompensation.EmploymentPeriodId
                        && (
                            !expectedCurrentId.HasValue
                            || compensation.Id !=
                                expectedCurrentId.Value
                        )
                        && (
                            !compensation.EffectiveTo.HasValue
                            || compensation.EffectiveTo.Value >=
                                newCompensation.EffectiveFrom
                        ),
                    cancellationToken);

        if (overlappingCompensationExists)
        {
            throw new EmployeeCompensationConcurrencyException(
                "Khoảng hiệu lực lương mới bị chồng lấn với cấu hình lương hiện có.");
        }

        AuditEntry createdAudit =
            _auditEntryFactory.Create(
                AuditAction.Created,
                AuditEntityTypes.EmployeeCompensation,
                newCompensation.Id,
                newCompensation.EmployeeId);

        EnsureAuditActorMatches(
            createdAudit,
            actorUserId,
            actorUsername);

        AuditEntry? updatedAudit =
            null;

        if (closedCompensation is not null)
        {
            updatedAudit =
                _auditEntryFactory.Create(
                    AuditAction.Updated,
                    AuditEntityTypes.EmployeeCompensation,
                    closedCompensation.Id,
                    closedCompensation.EmployeeId);

            EnsureAuditActorMatches(
                updatedAudit,
                actorUserId,
                actorUsername);

            int affectedRows =
                await dbContext
                    .EmployeeCompensations
                    .Where(
                        compensation =>
                            compensation.Id ==
                                closedCompensation.Id
                            && !compensation.EffectiveTo.HasValue)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters.SetProperty(
                                compensation =>
                                    compensation.EffectiveTo,
                                closedCompensation.EffectiveTo),
                        cancellationToken);

            if (affectedRows != 1)
            {
                throw new EmployeeCompensationConcurrencyException(
                    "Cấu hình lương hiện tại đã thay đổi trước khi lưu. Vui lòng làm mới dữ liệu.");
            }
        }

        await dbContext
            .EmployeeCompensations
            .AddAsync(
                newCompensation,
                cancellationToken);

        if (updatedAudit is not null)
        {
            await dbContext
                .AuditEntries
                .AddAsync(
                    updatedAudit,
                    cancellationToken);
        }

        await dbContext
            .AuditEntries
            .AddAsync(
                createdAudit,
                cancellationToken);

        try
        {
            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new EmployeeCompensationConcurrencyException(
                "Không thể lưu cấu hình lương vì dữ liệu đã thay đổi. Vui lòng làm mới và thử lại.",
                exception);
        }

        await transaction.CommitAsync(
            cancellationToken);
    }

    private static void ValidateInput(
        EmployeeCompensation? closedCompensation,
        EmployeeCompensation newCompensation,
        string actorUserId,
        string actorUsername)
    {
        if (!newCompensation.IsOpen)
        {
            throw new ArgumentException(
                "Cấu hình lương mới phải đang mở.",
                nameof(newCompensation));
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

        if (closedCompensation is null)
        {
            return;
        }

        if (closedCompensation.Id ==
            newCompensation.Id)
        {
            throw new ArgumentException(
                "Cấu hình lương mới phải có mã định danh khác.",
                nameof(newCompensation));
        }

        if (closedCompensation.EmployeeId !=
                newCompensation.EmployeeId
            || closedCompensation.EmploymentPeriodId !=
                newCompensation.EmploymentPeriodId)
        {
            throw new ArgumentException(
                "Cấu hình lương cũ và mới phải thuộc cùng nhân viên và giai đoạn làm việc.");
        }

        if (closedCompensation.IsOpen
            || !closedCompensation.EffectiveTo.HasValue)
        {
            throw new ArgumentException(
                "Cấu hình lương cũ phải được kết thúc trước khi lưu.");
        }

        DateOnly expectedNewStart =
            closedCompensation.EffectiveTo.Value
                .AddDays(
                    1);

        if (newCompensation.EffectiveFrom !=
            expectedNewStart)
        {
            throw new ArgumentException(
                "Ngày bắt đầu cấu hình lương mới phải ngay sau ngày kết thúc cấu hình lương cũ.");
        }
    }

    private static void EnsureAuditActorMatches(
        AuditEntry auditEntry,
        string actorUserId,
        string actorUsername)
    {
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
                "Người cập nhật lương không khớp với người dùng audit hiện tại.");
        }
    }
}
