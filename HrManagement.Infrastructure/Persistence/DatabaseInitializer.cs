using HrManagement.Infrastructure.Persistence.Demo;
using HrManagement.Infrastructure.Leave.Types;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Application.Employees.OrganizationAssignments;
using HrManagement.Application.Organization.Assignments;
using HrManagement.Infrastructure.Attendance.Schedules;
using Microsoft.EntityFrameworkCore;

namespace HrManagement.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly DatabaseInitializationOptions
        _options;

    private readonly DemoEmployeeSeedService
        _demoEmployeeSeedService;

    private readonly LeaveTypeSeedService
        _leaveTypeSeedService;

    private readonly WorkScheduleSeedService
        _workScheduleSeedService;

    private readonly
        IEmployeeOrganizationAssignmentBackfillService
            _employeeOrganizationAssignmentBackfillService;

    private readonly IEmployeeOrganizationBackfillService
        _employeeOrganizationBackfillService;

    private readonly IDbContextFactory<HrManagementDbContext>
        _dbContextFactory;

    private readonly IEmploymentHistoryBackfillService
        _employmentHistoryBackfillService;

    // Constructor
    public DatabaseInitializer(
    IDbContextFactory<HrManagementDbContext> dbContextFactory,
    IEmploymentHistoryBackfillService employmentHistoryBackfillService,
    IEmployeeOrganizationBackfillService employeeOrganizationBackfillService,
    IEmployeeOrganizationAssignmentBackfillService employeeOrganizationAssignmentBackfillService,
    WorkScheduleSeedService workScheduleSeedService, LeaveTypeSeedService leaveTypeSeedService,
    DatabaseInitializationOptions options, DemoEmployeeSeedService demoEmployeeSeedService)
    {
        _options =
            options;

        _demoEmployeeSeedService =
            demoEmployeeSeedService;

        _employeeOrganizationAssignmentBackfillService =
            employeeOrganizationAssignmentBackfillService;

        _dbContextFactory = dbContextFactory;

        _employmentHistoryBackfillService =
            employmentHistoryBackfillService;

        _employeeOrganizationBackfillService =
            employeeOrganizationBackfillService;

        _workScheduleSeedService =
            workScheduleSeedService;

        _leaveTypeSeedService =
            leaveTypeSeedService;
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
        }

        if (_options.DataMode ==
            ApplicationDataMode.Demo)
        {
            await _demoEmployeeSeedService
                .SeedAsync(
                    cancellationToken);
        }

        await _workScheduleSeedService
            .SeedAsync(
                cancellationToken);

        await _leaveTypeSeedService
            .SeedAsync(
                cancellationToken);

        await _employmentHistoryBackfillService
            .BackfillAsync(
                cancellationToken);

        await _employeeOrganizationBackfillService
            .BackfillAsync(
                cancellationToken);

        await _employeeOrganizationAssignmentBackfillService
            .BackfillAsync(
                cancellationToken);
    }
}
