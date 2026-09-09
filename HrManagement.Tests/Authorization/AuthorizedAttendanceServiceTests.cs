using HrManagement.Application.Attendance.Calculations;
using HrManagement.Application.Attendance.Corrections;
using HrManagement.Application.Attendance.Generation;
using HrManagement.Application.Attendance.Records;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedAttendanceServiceTests
{
    [Theory]
    [InlineData(ServiceKind.Punch)]
    [InlineData(ServiceKind.Recalculation)]
    [InlineData(ServiceKind.Generation)]
    public async Task
        MutationServices_RequireAttendanceEdit(
            ServiceKind serviceKind)
    {
        var guard =
            new TestAuthorizationGuard();

        switch (serviceKind)
        {
            case ServiceKind.Punch:
                await new AuthorizedAttendancePunchService(
                        new TestAttendancePunchService(),
                        guard)
                    .RecordAsync(
                        default!);
                break;

            case ServiceKind.Recalculation:
                await new AuthorizedAttendanceRecalculationService(
                        new TestAttendanceRecalculationService(),
                        guard)
                    .RecalculateAsync(
                        default!);
                break;

            case ServiceKind.Generation:
                await new AuthorizedDailyAttendanceGenerationService(
                        new TestDailyAttendanceGenerationService(),
                        guard)
                    .GenerateAsync(
                        default!);
                break;
        }

        Assert.Equal(
            PermissionCodes.AttendanceEdit,
            guard.LastPermissionCode);
    }

    [Fact]
    public async Task
        CorrectionQuery_RequiresAttendanceView()
    {
        var guard =
            new TestAuthorizationGuard();

        var service =
            new AuthorizedAttendanceCorrectionWorkspaceQueryService(
                new TestCorrectionQueryService(),
                guard);

        await service.GetAsync(
            Guid.NewGuid());

        Assert.Equal(
            PermissionCodes.AttendanceView,
            guard.LastPermissionCode);
    }

    [Fact]
    public async Task
        CorrectionPolicy_RequiresAttendanceEdit()
    {
        var authorizationService =
            new TestAuthorizationService();

        var policy =
            new PermissionAttendanceCorrectionAuthorizationPolicy(
                authorizationService);

        await policy.CanApplyAsync(
            new AttendanceCorrectionAuthorizationRequest(
                new HrManagement.Application.Authentication.AuthenticatedUser(
                    Guid.NewGuid().ToString("D"),
                    "test-user",
                    "Test User"),
                Guid.NewGuid(),
                Guid.NewGuid(),
                HrManagement.Domain.Attendance.Corrections
                    .AttendanceCorrectionKind.AddEvent));

        Assert.Equal(
            PermissionCodes.AttendanceEdit,
            authorizationService.LastPermissionCode);
    }

    public enum ServiceKind
    {
        Punch,
        Recalculation,
        Generation
    }

    private sealed class TestAuthorizationGuard
        : IAuthorizationGuard
    {
        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task RequirePermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LastPermissionCode =
                permissionCode;

            return Task.CompletedTask;
        }
    }

    private sealed class TestAuthorizationService
        : IAuthorizationService
    {
        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task<bool> HasPermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            LastPermissionCode =
                permissionCode;

            return Task.FromResult(
                true);
        }
    }

    private sealed class TestAttendancePunchService
        : IAttendancePunchService
    {
        public Task<RecordAttendancePunchResult> RecordAsync(
            RecordAttendancePunchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                default(RecordAttendancePunchResult)!);
        }
    }

    private sealed class TestAttendanceRecalculationService
        : IAttendanceRecalculationService
    {
        public Task<RecalculateAttendanceResult> RecalculateAsync(
            RecalculateAttendanceRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                default(RecalculateAttendanceResult)!);
        }
    }

    private sealed class TestDailyAttendanceGenerationService
        : IDailyAttendanceGenerationService
    {
        public Task<GenerateDailyAttendanceResult> GenerateAsync(
            GenerateDailyAttendanceRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                default(GenerateDailyAttendanceResult)!);
        }
    }

    private sealed class TestCorrectionQueryService
        : IAttendanceCorrectionWorkspaceQueryService
    {
        public Task<AttendanceCorrectionWorkspaceSnapshot?> GetAsync(
            Guid attendanceRecordId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                AttendanceCorrectionWorkspaceSnapshot?>(
                    null);
        }
    }
}
