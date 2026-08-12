using HrManagement.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using HrManagement.Application.Employees.EmploymentHistories;

namespace HrManagement.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IEmploymentHistoryBackfillService
    _employmentHistoryBackfillService;

    public DatabaseInitializer(
    IDbContextFactory<HrManagementDbContext> dbContextFactory,
    IEmploymentHistoryBackfillService employmentHistoryBackfillService)
    {
        _dbContextFactory = dbContextFactory;
        _employmentHistoryBackfillService =
            employmentHistoryBackfillService;
    }

    public async Task InitializeAsync(
    CancellationToken cancellationToken = default)
    {
        await using (
            HrManagementDbContext dbContext =
                await _dbContextFactory.CreateDbContextAsync(
                    cancellationToken))
        {
            await dbContext.Database.MigrateAsync(
                cancellationToken);

            if (!await dbContext.Employees
                    .AnyAsync(cancellationToken))
            {
                Employee[] employees =
                [
                    new(
                    Guid.Parse(
                        "11111111-1111-1111-1111-111111111111"),
                    "EMP001",
                    "Nguyễn Văn An",
                    "an.nguyen@example.com",
                    "0901000001",
                    new DateOnly(1995, 5, 20),
                    new DateOnly(2022, 3, 1),
                    "Nhân sự",
                    "Chuyên viên nhân sự",
                    EmployeeStatus.Active),
                new(
                    Guid.Parse(
                        "22222222-2222-2222-2222-222222222222"),
                    "EMP002",
                    "Trần Thị Bình",
                    "binh.tran@example.com",
                    "0901000002",
                    new DateOnly(1992, 8, 12),
                    new DateOnly(2021, 7, 15),
                    "Kế toán",
                    "Kế toán viên",
                    EmployeeStatus.Active),
                new(
                    Guid.Parse(
                        "33333333-3333-3333-3333-333333333333"),
                    "EMP003",
                    "Lê Minh Châu",
                    "chau.le@example.com",
                    "0901000003",
                    new DateOnly(1998, 1, 9),
                    new DateOnly(2023, 2, 10),
                    "Công nghệ thông tin",
                    "Lập trình viên",
                    EmployeeStatus.OnLeave),
                new(
                    Guid.Parse(
                        "44444444-4444-4444-4444-444444444444"),
                    "EMP004",
                    "Phạm Quốc Dũng",
                    "dung.pham@example.com",
                    "0901000004",
                    new DateOnly(1989, 11, 3),
                    new DateOnly(2020, 10, 5),
                    "Kinh doanh",
                    "Trưởng nhóm kinh doanh",
                    EmployeeStatus.Active),
                new(
                    Guid.Parse(
                        "55555555-5555-5555-5555-555555555555"),
                    "EMP005",
                    "Võ Thu Hà",
                    null,
                    null,
                    new DateOnly(1996, 4, 18),
                    new DateOnly(2019, 6, 20),
                    "Hành chính",
                    "Chuyên viên hành chính",
                    EmployeeStatus.Inactive)
                ];

                await dbContext.Employees.AddRangeAsync(
                    employees,
                    cancellationToken);

                await dbContext.SaveChangesAsync(
                    cancellationToken);
            }
        }

        await _employmentHistoryBackfillService
            .BackfillAsync(
                cancellationToken);
    }
}
